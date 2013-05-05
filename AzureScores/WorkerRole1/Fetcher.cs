using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using WorkerRole1.Helpers;
using System.Diagnostics;

namespace WorkerRole1
{
    public class Fetcher
    {
        private readonly Dictionary<long, string> pages;
        private readonly BrowserSession browserSession;

        public Fetcher()
        {
            pages = new Dictionary<long, string>
                {
                    {1, "http://www.rugby.be/new/competition.cfm?div_id=10113"},
                    {2, "http://www.rugby.be/new/competition.cfm?div_id=10122"},
                    {3, "http://www.rugby.be/new/competition.cfm?div_id=10133"},
                    {4, "http://www.rugby.be/new/competition.cfm?div_id=10142"},
                    {5, "http://www.rugby.be/new/competition.cfm?div_id=10157"},
                    {6, "http://www.rugby.be/new/competition.cfm?div_id=10114"}
                };

            browserSession = new BrowserSession();
            //Force a first load to the correct season, calls after this will go to the correct page
            browserSession.GetDoc("http://www.rugby.be/new/competition.cfm?p_year=12");
        }

        public void LoadGamesForAllDivisions()
        {
            foreach (var page in pages)
            {
                LoadGamesPerDivision(browserSession, page);
            }
        }

        private void LoadGamesPerDivision(BrowserSession b, KeyValuePair<long, string> page)
        {
            try
            {
                var teams = GetTeams(1, page.Key);

                int take = 1;
                take = take + teams.Count / 2;

                if (teams.Count % 2 > 0)
                    take++;

                var doc = b.GetDoc(page.Value);

                //Table  => Games table
                var table =
                    doc.DocumentNode.Descendants("table")
                       .FirstOrDefault(
                           t => t.Attributes.Contains("class") && t.Attributes["class"].Value.Contains("clubentrees"));
                if (table != null)
                {

                    using (var context = new ScoresEntities())
                    {
                        int skip = 1;
                        var rows = table.Descendants("tr").Skip(skip).Take(take).ToList();

                        while (rows.Any())
                        {
                            var dateRow = rows[0];
                            DateTime? date = ProcessDateRow(dateRow);

                            if (date.HasValue)
                            {
                                foreach (var row in rows.Skip(1))
                                {
                                    Trace.WriteLine(row.InnerText);
                                    games game = ProcessGameRow(row, date.Value, teams, context, page);

                                    if (game != null && game.GameId <= 0)
                                        context.games.Add(game);
                                }
                            }
                            Trace.WriteLine("----------");
                            skip = skip + take + 1;
                            rows = table.Descendants("tr").Skip(skip).Take(take + 1).ToList();
                        }
                        context.SaveChanges();
                    }
                }
                else
                    Trace.WriteLine("---WRONG---");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("There was an error fetching division {0}", page.Key));
                throw;
            }
        }

        private DateTime? ProcessDateRow(HtmlNode row)
        {
            string dateValue = row.Descendants("td").First().InnerText.CleanHtml().Trim();
            if (!string.IsNullOrEmpty(dateValue))
            {
                string datePart = dateValue.Contains(",") ? dateValue.Substring(0, dateValue.IndexOf(',')) : dateValue;
                if (!string.IsNullOrEmpty(datePart))
                {
                    DateTime result;
                    if (DateTime.TryParse(datePart, out result))
                    {
                        Trace.WriteLine(string.Format("-- Date: {0} --", result));
                        return result;
                    }
                }
            }
            return null;
        }

        private games ProcessGameRow(HtmlNode row, DateTime date, List<Teams> teams, ScoresEntities context, KeyValuePair<long, string> page)
        {
            games game = null;
            string gameNumber;
            //Find the game number
            var gameNrNode = row.Descendants("td").FirstOrDefault();
            if (gameNrNode != null && !string.IsNullOrEmpty(gameNrNode.InnerText.CleanHtml().Trim()))
            {
                gameNumber = gameNrNode.InnerText.CleanHtml();
                game = GetGameByGameNumber(gameNumber, context);
            }
            else
            {
                return null;
            }

            if (game == null)
            {
                game = new games();
                game.GameNumber = gameNumber;
                game.Date = date;
                //Check the date

                var newDateNode = row.Descendants("td").LastOrDefault();
                if (newDateNode != null && !string.IsNullOrEmpty(newDateNode.InnerText))
                {
                    DateTime newDate;
                    if (DateTime.TryParse(newDateNode.InnerText.CleanHtml(), out newDate))
                        game.Date = newDate;
                }


                //TODO: change to correct divisionId
                game.Divisionid = page.Key;
                game.SeasonId = 1;

                //Find the first team
                var team1Node = row.Descendants("td").Skip(1).Take(1).FirstOrDefault();
                if (team1Node != null && !string.IsNullOrEmpty(team1Node.InnerText))
                {
                    var team1 = teams.FirstOrDefault(t => t.Team == team1Node.InnerText.CleanHtml().Trim());
                    if (team1 != null)
                        game.Team1Id = team1.TeamId;
                }

                //find the second team
                var team2Node = row.Descendants("td").Skip(2).Take(1).FirstOrDefault();
                if (team2Node != null && !string.IsNullOrEmpty(team2Node.InnerText))
                {
                    var team2 = teams.FirstOrDefault(t => t.Team == team2Node.InnerText.CleanHtml().Trim());
                    if (team2 != null)
                        game.Team2Id = team2.TeamId;
                }
            }

            //Find the score
            var scoreNode = row.Descendants("td").Skip(4).Take(1).FirstOrDefault();
            int team1Score = 0;
            int team2Score = 0;

            if (scoreNode != null && !string.IsNullOrEmpty(scoreNode.InnerText.CleanHtml().Trim()))
            {
                string[] scores = scoreNode.InnerText.CleanHtml().Trim().Split('-');
                if (!int.TryParse(scores[0].Trim(), out team1Score))
                {
                    //Check for FF
                    if (scores[0].Trim().Equals("FF", StringComparison.InvariantCultureIgnoreCase))
                    {
                        team1Score = -1;
                    }
                }
                if (!int.TryParse(scores[1].Trim(), out team2Score))
                {
                    //Check for FF
                    if (scores[1].Trim().Equals("FF", StringComparison.InvariantCultureIgnoreCase))
                    {
                        team2Score = -1;
                    }
                }
            }
            game.Team1Score = team1Score;
            game.Team2Score = team2Score;


            //Find the try count
            int team1Try = 0;
            int team2Try = 0;

            var tryNode = row.Descendants("td").Skip(5).Take(1).FirstOrDefault();
            if (tryNode != null && !string.IsNullOrEmpty(tryNode.InnerText.CleanHtml().Trim()))
            {
                string[] scores = tryNode.InnerText.CleanHtml().Trim().Split('-');
                int.TryParse(scores[0].Trim(), out team1Try);
                int.TryParse(scores[1].Trim(), out team2Try);
            }
            game.Team1Tries = team1Try;
            game.Team2Tries = team2Try;

            return game;
        }

        private games GetGameByGameNumber(string gameNumber, ScoresEntities context)
        {
            return context.games.FirstOrDefault(g => g.GameNumber == gameNumber);
        }

        private List<Teams> GetTeams(long seasonId, long divisionId)
        {
            using (var context = new ScoresEntities())
            {
                return
                    context.TeamSeasonDivision.Where(td => td.DivisionId == divisionId && td.SeasonId == seasonId)
                           .Select(td => td.Teams)
                           .ToList();
            }
        }


    }
}
