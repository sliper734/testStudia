using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using studiaT_G_test.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace studiaT_G_test.Controllers
{
    [EnableCors]
    [ApiController]
    [Route("api/[controller]")]
    public class MinesweeperController : Controller
    {
        private ApplicationContext db;
        private GameInfoModel gameInfoModel;
        private List<string[]> map;
        public MinesweeperController(ApplicationContext context)
        {
            db = context;
        }

        private string MapToStr(List<string[]> m, int width, int height)
        {
            string strMap = "";
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    strMap += m[i][j] + ",";
                    if (j + 1 == height)
                    {
                        strMap = strMap.Remove(strMap.Length - 1, 1);
                    }
                }
                strMap += "/r/n";
            }
            return strMap;
        }

        private List<string[]> StrToMap(string m)
        {
            List<string[]> listMap = new List<string[]>();

            List<string> r = m.Split("/r/n").ToList();
            var q = r.Last();
            r.Remove(r.Last());
            for (int i = 0; i < r.Count; i++)
            {
                listMap.Add(r[i].Split(','));
            }

            return listMap;
        }

        private void CreateMap(NewGameModel gameModel)
        {
            CreateEmptyMap(gameModel.width, gameModel.height);
            AddMines(gameModel.mines_count);
            UpdateMinesAround(gameModel.width, gameModel.height);
        }

        private void CreateEmptyMap(int width, int height)
        {
            map = new List<string[]>();
            for (int i = 0; i < width; i++)
            {
                map.Add(new string[height]);
                for (int j = 0; j < height; j++)
                {
                    map[i][j] = "0";
                }
            }
            
        }

        private void AddMines(int mines_count)
        {
            Random rnd = new Random();
            for (int i = 0; i < mines_count; i++)
            {
                var row = rnd.Next(0, 10);
                var col = rnd.Next(0, 10);
                while (map[row][col] =="X")
                {
                    row = rnd.Next(0, 10);
                    col = rnd.Next(0, 10);
                }
                map[row][col] = "X";
            }
        }

        private void UpdateMinesAround(int width, int height)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if(map[i][j] == "X")
                    {
                        for (int c = i-1; c <= i+1; c++)
                        {
                            for (int k = j-1; k <= j+1; k++)
                            {
                                if (!cellValid(width, height, c, k)) continue;
                                if (map[c][k] == "X") continue;
                                int cell = int.Parse(map[c][k]) + 1;
                                map[c][k] = cell.ToString();
                            }
                        }
                    }
                }
            }
        }

        private void OpenZeroCell(int row, int col)
        {
            for (int i = row - 1; i <= row + 1; i++) 
            {
                for (int j = col - 1; j <= col+1; j++)
                {
                    if (!cellValid(gameInfoModel.width, gameInfoModel.height, i, j)) continue;
                    if (i == row && j == col) continue;
                    if (map[i][j] == "0" && gameInfoModel.field[i][j] != map[i][j])
                    {
                        gameInfoModel.field[i][j] = map[i][j];
                        OpenZeroCell(i, j);
                    }
                    if(map[i][j] != "0")
                    {
                        gameInfoModel.field[i][j] = map[i][j];
                    }
                }
            }
        }

        private bool cellValid(int width, int height, int row, int col)
        {
            return row >= 0 && col >= 0 && row < width && col < height;
        }

        [HttpPost("new")]
        public JsonResult New(NewGameModel gameModel)
        {
            if(gameModel.width > 30 || gameModel.height > 30)
            {
                return new JsonResult(new ErrorModel() { error = "Размер поля превышает максимальный (максимальный размер 30 х 30)" })
                {
                    StatusCode = StatusCodes.Status400BadRequest,

                };
            }
            if ((gameModel.width * gameModel.height - 1) <= gameModel.mines_count) 
            {
                return new JsonResult(new ErrorModel() { error = "Количество мин превышает максимальное количество (максимальное количество для данного поля  " + (gameModel.width * gameModel.height - 1) + " )" })
                {
                    StatusCode = StatusCodes.Status400BadRequest,

                };
            }
            CreateMap(gameModel);
            gameInfoModel = new GameInfoModel();
            gameInfoModel.game_id = Guid.NewGuid().ToString();
            gameInfoModel.width = gameModel.width;
            gameInfoModel.height = gameModel.height;
            gameInfoModel.mines_count = gameModel.mines_count;
            gameInfoModel.completed = false;
            gameInfoModel.field = new List<string[]>();
            for (int i = 0; i < gameModel.width; i++)
            {
                gameInfoModel.field.Add(new string[gameModel.height]);
                for (int j = 0; j < gameModel.height; j++)
                {
                    gameInfoModel.field[i][j] = " ";
                }
            }
            DbInfoModel dbInfoModel = new DbInfoModel();
            dbInfoModel.game_id = gameInfoModel.game_id;
            dbInfoModel.width = gameInfoModel.width;
            dbInfoModel.height = gameInfoModel.height;
            dbInfoModel.mines_count = gameInfoModel.mines_count;
            dbInfoModel.completed = gameInfoModel.completed;
            dbInfoModel.field = MapToStr(gameInfoModel.field, gameInfoModel.width, gameInfoModel.height);
            dbInfoModel.map = MapToStr(map, gameInfoModel.width, gameInfoModel.height);
            db.DbIModels.Add(dbInfoModel);
            db.SaveChanges();
            return Json(gameInfoModel);
        }

        [HttpGet("get")]
        public JsonResult Get()
        {
            return new JsonResult("qwer");
        }

        [HttpPost("turn")]
        public JsonResult Turn(GameTurnModel gameTurnModel)
        {
            DbInfoModel dbInfoModel = db.DbIModels.FirstOrDefault(p => p.game_id == gameTurnModel.game_id);
            if (dbInfoModel == null)
            {
                return new JsonResult(new ErrorModel() { error = "Данная игра не была найдена" })
                {
                    StatusCode = StatusCodes.Status400BadRequest,

                };
            }
            if (dbInfoModel.completed)
            {
                return new JsonResult(new ErrorModel() { error= "Игра была уже закончена. Начните новую" })
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    
                };
            }

            

            gameInfoModel = new GameInfoModel();
            List<string[]> field = StrToMap(dbInfoModel.field);
            map = StrToMap(dbInfoModel.map);
            field[gameTurnModel.row][gameTurnModel.col] = map[gameTurnModel.row][gameTurnModel.col];

            if (map[gameTurnModel.row][gameTurnModel.col] == "X")
            {
                dbInfoModel.completed = true;
                field = map;
                for (int i = 0; i < dbInfoModel.width; i++)
                {
                    for (int j = 0; j < dbInfoModel.height; j++)
                    {
                        if (field[i][j] == "X" && (i != gameTurnModel.row || j != gameTurnModel.col))
                            field[i][j] = "M";
                    }
                }
                dbInfoModel.field = MapToStr(field, dbInfoModel.width, dbInfoModel.height);
                db.DbIModels.Update(dbInfoModel);
                db.SaveChanges();
                gameInfoModel.game_id = dbInfoModel.game_id;
                gameInfoModel.width = dbInfoModel.width;
                gameInfoModel.height = dbInfoModel.height;
                gameInfoModel.mines_count = dbInfoModel.mines_count;
                gameInfoModel.completed = dbInfoModel.completed;
                gameInfoModel.field = field;
                return Json(gameInfoModel);
            }

            if (map[gameTurnModel.row][gameTurnModel.col] == "0")
            {
                gameInfoModel.field = field;
                gameInfoModel.width = dbInfoModel.width;
                gameInfoModel.height = dbInfoModel.height;
                OpenZeroCell(gameTurnModel.row, gameTurnModel.col);
            }

            bool isGameOver = true;

            for (int i = 0; i < dbInfoModel.width; i++)
            {
                if (!isGameOver)
                    break;
                for (int j = 0; j < dbInfoModel.height; j++)
                {
                    if(field[i][j] != map[i][j])
                    {
                        isGameOver = false;
                        break;
                    }
                }
            }

            if(isGameOver)
            {
                for (int i = 0; i < dbInfoModel.width; i++)
                {
                    for (int j = 0; j < dbInfoModel.height; j++)
                    {
                        if (field[i][j] == "X")
                            field[i][j] = "M";
                    }
                }
                dbInfoModel.completed = true;
            }
            dbInfoModel.field = MapToStr(field, dbInfoModel.width, dbInfoModel.height);
            db.DbIModels.Update(dbInfoModel);
            db.SaveChanges();
            gameInfoModel.game_id = dbInfoModel.game_id;
            gameInfoModel.width = dbInfoModel.width;
            gameInfoModel.height = dbInfoModel.height;
            gameInfoModel.mines_count = dbInfoModel.mines_count;
            gameInfoModel.completed = dbInfoModel.completed;
            gameInfoModel.field = field;

            return Json(gameInfoModel);
        }
    }
}