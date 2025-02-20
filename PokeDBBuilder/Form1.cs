using HtmlAgilityPack;
using OpenCCNET;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using static System.Windows.Forms.LinkLabel;
using Application = System.Windows.Forms.Application;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace PokeDBBuilder
{
    public partial class Form1 : Form
    {
        private readonly static string pokeDB = "PokeDB.sqlite";
        private readonly static string DBPokeTable = "pokedata";
        private readonly static string DBMegaTable = "pokeMegadata";
        private readonly static string[] DBTables = [DBPokeTable, DBMegaTable];
        private static string pokeNameFilePath = string.Empty;
        private static string pokeMegaNameFilePath = string.Empty;
        private static string pokeDataFilePath = string.Empty;
        private static string pokeEvolutionFilePath = string.Empty;

        private static List<string> pokeNameList = [];
        private static List<string> pokeMegaNameList = [];

        Dictionary<int, string[]> megaDict = [];
        Dictionary<int, string[]> nationalNumberAndNamesDict = [];
        Dictionary<int, string[]> nationalNumberAndMegaNamesDict = [];
        private static List<PokeData> pokeDatas = [];
        private static List<PokeData> megaPokeDatas = [];

        System.Windows.Forms.Timer timer;
        DateTime startTime;
        TimeSpan elapsedTime;

        public Form1()
        {
            InitializeComponent();

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            elapsedTime = DateTime.Now - startTime;
            Text = "PokeDBBuilder" + "  已运行：" + elapsedTime.ToString(@"hh\:mm\:ss");
        }

        private void CleanFormTitle()
        {
            Text = "PokeDBBuilder";
        }

        private static void DeleteDB()
        {
            string dbFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), pokeDB);

            if (File.Exists(dbFilePath))
            {
                File.Delete(dbFilePath);
            }
        }

        private SQLiteConnection CreateSQLiteConnection()
        {
            string dbFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), pokeDB);
            string connectionString = $"Data Source={dbFilePath};Version=3;";
            return new SQLiteConnection(connectionString);
        }

        private void ExecuteSQLiteCommand(string queryIn)
        {
            string dbFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), pokeDB);
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbFilePath};Version=3;"))
            {
                connection.Open();
                SQLiteCommand command = new SQLiteCommand(queryIn, connection);
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        private static string SQLiteEscapingSpecialCharacters(string input)
        {
            return input.Replace("\\", "\\\\").Replace("'", "''").Replace("\"", "\"\"").Replace("?", "??");
        }

        private void CreateTable()
        {
            var connection = CreateSQLiteConnection();
            connection.Open();

            foreach (var table in DBTables)
            {
                // 创建表
                var createTable = new SQLiteCommand(
                    "CREATE TABLE IF NOT EXISTS " + table + " (" +
                    "id INTEGER PRIMARY KEY, " +
                    "nationalNumber INTEGER, " +
                    "name TEXT, " +
                    "type TEXT, " +
                    "abilities TEXT, " +
                    "baseStats TEXT, " +
                    "evolutionaryStage INTEGER, " +
                    "ifFinalStage BOOL, " +
                    "ifMegaForm BOOL, " +
                    "ifLegendary BOOL)"
                    , connection);
                createTable.ExecuteNonQuery();
            }

            connection.Close();
        }

        private void InsertPokeName(string tableName, List<string> nameList)
        {
            var connection = CreateSQLiteConnection();
            connection.Open();

            // 插入数据
            foreach (var pokeName in nameList)
            {
                var insertData = new SQLiteCommand("INSERT INTO " + tableName + " (name) VALUES ('" + pokeName + "')", connection);
                try
                {
                    insertData.ExecuteNonQuery();
                }
                catch
                {
                    MessageBox.Show(pokeName, "Debug-InsertPokeName()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            connection.Close();
        }

        private void InsertPokeDexNumber()
        {
            var connection = CreateSQLiteConnection();
            connection.Open();

            var updatePokeDexNumber = new SQLiteCommand("UPDATE " + DBPokeTable + " SET nationalNumber = ID", connection);
            updatePokeDexNumber.ExecuteNonQuery();

            if (pokeMegaNameFilePath == string.Empty)
            {
                // 复制到Mega表
                List<int> megaList = PokeData.getMegaList();
                string megaValues = string.Join(", ", megaList);

                var query = $"INSERT INTO {DBMegaTable} SELECT * FROM {DBPokeTable} WHERE nationalNumber IN ({megaValues})";
                var copyMega = new SQLiteCommand(query, connection);
                try
                {
                    copyMega.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(query + "\n" + ex.Message, "Debug-InsertPokeDexNumber()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            connection.Close();
        }

        private void UpdateMegaName()
        {
            var connection = CreateSQLiteConnection();
            connection.Open();

            // 查询name列的值，并按逗号分割
            string query = "SELECT name FROM " + DBMegaTable;
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader["name"].ToString();
                        string updatedName = UpdateName(name);

                        // 更新name列的值
                        string updateQuery = $"UPDATE {DBMegaTable} SET name = '{updatedName}' WHERE name = '{name}'";
                        using (SQLiteCommand updateCommand = new SQLiteCommand(updateQuery, connection))
                        {
                            updateCommand.ExecuteNonQuery();
                        }
                    }
                }
            }

            connection.Close();
        }

        private static string UpdateName(string name)
        {
            string[] elements = name.Split(',');
            for (int i = 0; i < elements.Length; i++)
            {
                if (i == 7) // 第8个元素
                {
                    elements[i] = "超级" + elements[i];
                }
                else if (i == 8)
                {
                    elements[i] = "超級" + elements[i];
                }
                else
                {
                    elements[i] = elements[i] + " Mega";
                }
            }
            return string.Join(",", elements);
        }

        private void GetNationalNumberAndNameDict(string DBTable, Dictionary<int, string[]> dict)
        {
            dict.Clear();

            var connection = CreateSQLiteConnection();
            connection.Open();

            string query = $"SELECT nationalNumber, name FROM {DBTable}";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int _nationalNumber = Int16.Parse(reader["nationalNumber"].ToString());
                        string _name = reader["name"].ToString();
                        string[] _nameList = _name.Split(',');

                        dict[_nationalNumber] = _nameList;
                    }
                }
            }

            connection.Close();
        }

        private void GetNationalNumberAndNameDict(string DBTable, Dictionary<int, string[]> dict, List<int> dexList)
        {
            dict.Clear();

            var connection = CreateSQLiteConnection();
            connection.Open();

            string dexValues = string.Join(", ", dexList);
            string query = $"SELECT nationalNumber, name FROM {DBTable} WHERE nationalNumber IN ({dexValues})";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int _nationalNumber = Int16.Parse(reader["nationalNumber"].ToString());
                        string _name = reader["name"].ToString();
                        string[] _nameList = _name.Split(',');

                        dict[_nationalNumber] = _nameList;
                    }
                }
            }

            connection.Close();
        }

        private void UpdatePokeDatas(string DBTable, List<PokeData> list)
        {
            var connection = CreateSQLiteConnection();
            connection.Open();

            foreach (var poke in list)
            {
                var nationalNumber = poke.nationalNumber;
                var type = string.Join(",", poke.type);
                var abilities = string.Join(",", poke.abilities);
                var baseStats = string.Join(", ", poke.baseStats);
                var evolutionaryStage = poke.evolutionaryStage.ToString();
                var ifFinalStage = poke.ifFinalStage.ToString();
                var ifMegaForm = poke.ifMegaForm.ToString();
                var ifLegendary = poke.ifLegendary.ToString();

                string query = $"UPDATE {DBTable} SET type = '{type}', abilities = '{abilities}', baseStats = '{baseStats}', evolutionaryStage = '{evolutionaryStage}'," +
                    $" ifFinalStage = '{ifFinalStage}', ifMegaForm = '{ifMegaForm}', ifLegendary = '{ifLegendary}' WHERE nationalNumber = {nationalNumber}";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            connection.Close();
        }

        // UI
        private void LLB_File_1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (pokeNameFilePath != "")
            {
                // 将filePath中的文件夹路径提取出来
                string[] fileName = pokeNameFilePath.Split("\\");
                string fileFolderPath = pokeNameFilePath.Replace(fileName[^1], "");

                // 打开文件夹
                try
                {
                    System.Diagnostics.Process.Start("explorer", fileFolderPath);
                }
                catch
                {
                    MessageBox.Show("路径错误！请确认路径是否存在！", "路径错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LLB_File_2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (pokeDataFilePath != "")
            {
                // 将filePath中的文件夹路径提取出来
                string[] fileName = pokeDataFilePath.Split("\\");
                string fileFolderPath = pokeDataFilePath.Replace(fileName[^1], "");

                // 打开文件夹
                try
                {
                    System.Diagnostics.Process.Start("explorer", fileFolderPath);
                }
                catch
                {
                    MessageBox.Show("路径错误！请确认路径是否存在！", "路径错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LLB_File_3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (pokeMegaNameFilePath != "")
            {
                // 将filePath中的文件夹路径提取出来
                string[] fileName = pokeMegaNameFilePath.Split("\\");
                string fileFolderPath = pokeMegaNameFilePath.Replace(fileName[^1], "");

                // 打开文件夹
                try
                {
                    System.Diagnostics.Process.Start("explorer", fileFolderPath);
                }
                catch
                {
                    MessageBox.Show("路径错误！请确认路径是否存在！", "路径错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LLB_File_4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (pokeEvolutionFilePath != "")
            {
                // 将filePath中的文件夹路径提取出来
                string[] fileName = pokeEvolutionFilePath.Split("\\");
                string fileFolderPath = pokeEvolutionFilePath.Replace(fileName[^1], "");

                // 打开文件夹
                try
                {
                    System.Diagnostics.Process.Start("explorer", fileFolderPath);
                }
                catch
                {
                    MessageBox.Show("路径错误！请确认路径是否存在！", "路径错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BTN_Open_F1_Click(object sender, EventArgs e)
        {
            // 打开文件
            OpenFileDialog openFileDialog = new()
            {
                FileName = "选择一个txt文件",
                Filter = "txt文件(*.txt)|*.txt",
                Title = "打开txt文件"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 更新文件夹
                pokeNameFilePath = openFileDialog.FileName;

                // 更新文件名
                string[] fileName = openFileDialog.FileName.Split("\\");
                string openFileName = fileName[^1];
                LLB_File_1.Text = openFileName;
            }
        }

        private void BTN_Open_F2_Click(object sender, EventArgs e)
        {
            // 打开文件
            OpenFileDialog openFileDialog = new()
            {
                FileName = "选择一个txt文件",
                Filter = "txt文件(*.txt)|*.txt",
                Title = "打开txt文件"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 更新文件夹
                pokeDataFilePath = openFileDialog.FileName;

                // 更新文件名
                string[] fileName = openFileDialog.FileName.Split("\\");
                string openFileName = fileName[^1];
                LLB_File_2.Text = openFileName;
            }
        }

        private void BTN_Open_F3_Click(object sender, EventArgs e)
        {
            // 打开文件
            OpenFileDialog openFileDialog = new()
            {
                FileName = "选择一个txt文件",
                Filter = "txt文件(*.txt)|*.txt",
                Title = "打开txt文件"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 更新文件夹
                pokeMegaNameFilePath = openFileDialog.FileName;

                // 更新文件名
                string[] fileName = openFileDialog.FileName.Split("\\");
                string openFileName = fileName[^1];
                LLB_File_3.Text = openFileName;
            }
        }

        private void BTN_Open_F4_Click(object sender, EventArgs e)
        {
            // 打开文件
            OpenFileDialog openFileDialog = new()
            {
                FileName = "选择一个txt文件",
                Filter = "txt文件(*.txt)|*.txt",
                Title = "打开txt文件"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 更新文件夹
                pokeEvolutionFilePath = openFileDialog.FileName;

                // 更新文件名
                string[] fileName = openFileDialog.FileName.Split("\\");
                string openFileName = fileName[^1];
                LLB_File_4.Text = openFileName;
            }
        }

        // 读取名称
        private async Task ReadPokeNameFromFile()
        {
            pokeNameList.Clear();

            // 读取源文件
            var inputStream = new StreamReader(pokeNameFilePath, Encoding.UTF8);
            var input = await inputStream.ReadToEndAsync();
            inputStream.Close();

            // 按行分割
            string[] inputString = input.Split(['\r', '\n']);

            List<string> sourceList = [];

            // 去除空行和制表符
            foreach (var line in inputString)
            {
                if (line != "")
                {
                    if (line.Contains('\t'))
                    {
                        sourceList.Add(line.Trim(['\t']));
                    }
                    else
                    {
                        sourceList.Add(line);
                    }
                }
            }

            // 更新pokeNameList
            foreach (var source in sourceList)
            {
                string[] temp = source.Split(',');
                List<string> list = [];

                try
                {
                    for (int i = 2; i < 19; i += 2)
                    {
                        list.Add(temp[i]);
                    }
                }
                catch
                {
                    MessageBox.Show("名称添加失败，请检查数据文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 去掉日语跟韩语的发音标注
                for (int i = 0; i < list.Count; i++)
                {
                    string[] _temp = list[i].Split(" (");
                    if (_temp.Length > 1)
                    {
                        list[i] = _temp[0];
                    }

                    // 转义字符
                    list[i] = SQLiteEscapingSpecialCharacters(list[i]);
                }

                StringBuilder sb = new();
                foreach (var line in list)
                {
                    sb.Append(line);
                    sb.Append(',');
                }

                // 删除最后一个逗号
                if (sb.Length > 0)
                {
                    sb.Length--;
                }

                string output = sb.ToString();
                pokeNameList.Add(output);
            }
        }

        // 读取Mega名称
        // 数据源应当带有全国图鉴编号
        // 需要适配实际数据源
        private async Task ReadMegaPokeNameFromFile()
        {
            pokeMegaNameList.Clear();

            // 读取源文件
            var inputStream = new StreamReader(pokeMegaNameFilePath, Encoding.UTF8);
            var input = await inputStream.ReadToEndAsync();
            inputStream.Close();

            // 按行分割
            string[] inputString = input.Split(['\r', '\n']);

            List<string> sourceList = [];

            // 去除空行和制表符
            foreach (var line in inputString)
            {
                if (line != "")
                {
                    if (line.Contains('\t'))
                    {
                        sourceList.Add(line.Trim(['\t']));
                    }
                    else
                    {
                        sourceList.Add(line);
                    }
                }
            }

            // 更新pokeMegaNameList
            foreach (var source in sourceList)
            {
                string[] temp = source.Split(',');
                List<string> list = [];

                try
                {
                    for (int i = 2; i < 19; i += 2)
                    {
                        list.Add(temp[i]);
                    }
                }
                catch
                {
                    MessageBox.Show("名称添加失败，请检查数据文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 去掉日语跟韩语的发音标注
                for (int i = 0; i < list.Count; i++)
                {
                    string[] _temp = list[i].Split(" (");
                    if (_temp.Length > 1)
                    {
                        list[i] = _temp[0];
                    }

                    // 转义字符
                    list[i] = SQLiteEscapingSpecialCharacters(list[i]);
                }

                StringBuilder sb = new();
                foreach (var line in list)
                {
                    sb.Append(line);
                    sb.Append(',');
                }

                // 删除最后一个逗号
                if (sb.Length > 0)
                {
                    sb.Length--;
                }

                string output = sb.ToString();
                pokeMegaNameList.Add(output);
            }
        }

        // 读取获取种族值等数据
        private async Task ReadPokeDataFromFile()
        {
            pokeDatas.Clear();
            megaPokeDatas.Clear();

            // 读取源文件
            var inputStream = new StreamReader(pokeDataFilePath, Encoding.UTF8);
            var input = await inputStream.ReadToEndAsync();
            inputStream.Close();

            // 按行分割
            string[] separator = ["--------------------------------------------"];
            string[] inputString = input.Split(separator, StringSplitOptions.None);

            List<List<string>> pokeDataLists = [];

            // 去除空行和制表符
            foreach (var line in inputString)
            {
                if (line != "")
                {
                    List<string> pokeDataList = [];
                    string[] _inputString = line.Split(['\r', '\n']);
                    foreach (var line2 in _inputString)
                    {
                        if (line2 != "")
                        {
                            if (line2.Contains('\t'))
                            {
                                pokeDataList.Add(line2.Trim(['\t']));
                            }
                            else
                            {
                                pokeDataList.Add(line2);
                            }
                        }
                    }
                    pokeDataLists.Add(pokeDataList);
                }
            }

            // 分开处理
            List<List<string>> normalPokes = [];
            List<List<string>> extraPokes = [];
            List<List<string>> megaPokes = [];

            foreach (var item in pokeDataLists)
            {
                var number = Int16.Parse(item[0].Trim());

                if (number < 808)
                {
                    normalPokes.Add(item);
                }
                else
                {
                    extraPokes.Add(item);
                }
            }

            // 筛选mega
            GetNationalNumberAndNameDict(DBPokeTable, megaDict, PokeData.getMegaList());

            foreach (var item in extraPokes)
            {
                var pokeName = item[1].Trim().Split(' ')[0];

                foreach (var kvp in megaDict)
                {
                    if (kvp.Value[7] == pokeName)
                    {
                        megaPokes.Add(item);
                    }
                }
            }

            // 更新普通数据
            List<string> pokeNameList = [];
            Debug.WriteLine($"normalPokes.Count: {normalPokes.Count}");
            foreach (var item in normalPokes)
            {
                var pokeName = item[1].Trim();
                if (!pokeNameList.Contains(pokeName))
                {
                    pokeNameList.Add(pokeName);
                }
                else
                {
                    continue;
                }

                var poke = new PokeData(pokeName);

                // 获取属性
                Debug.WriteLine($"获取属性");
                string[] temp_type = item[7].Split(':')[1].Split('/');
                foreach (var line in temp_type)
                {
                    if (line != "")
                    {
                        poke.type.Add(line.Trim());
                    }
                }

                // 获取特性
                Debug.WriteLine($"获取特性");
                string[] temp_abilities = item[8].Split(':')[1].Split('|');
                foreach (var line in temp_abilities)
                {
                    if (line != "")
                    {
                        var ability = line.Trim().Split(' ')[0];

                        if (poke.abilities.Count < 1 || !poke.abilities.Contains(ability))
                        {
                            poke.abilities.Add(ability);
                        }
                    }
                }

                // 获取baseStats
                Debug.WriteLine($"获取baseStats");
                string[] temp_baseStats = item[5].Split(':')[1].Trim().Split('.');
                Debug.WriteLine($"{poke.name}");
                for (int i = 0; i < temp_baseStats.Length; i++)
                {
                    try
                    {
                        //poke.baseStats[i] = Int16.Parse(temp_baseStats[i]);
                        poke.baseStats.Add(Int16.Parse(temp_baseStats[i]));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"baseStats不是一个有效的值: {ex.Message}");
                        MessageBox.Show("baseStats不是一个有效的值", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                // 获取进化阶段
                Debug.WriteLine($"获取进化阶段");
                string stage = item[2].Split(':')[1].Trim();
                try
                {
                    poke.evolutionaryStage = Int16.Parse(stage);
                }
                catch
                {
                    MessageBox.Show("进化阶段不是一个有效的值", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // 查询全国图鉴编号
                Debug.WriteLine($"查询全国图鉴编号");
                GetNationalNumberAndNameDict(DBPokeTable, nationalNumberAndNamesDict);

                foreach (var kvp in nationalNumberAndNamesDict)
                {
                    if (pokeName == kvp.Value[7])
                    {
                        poke.nationalNumber = kvp.Key;
                    }
                }

                // 判断是否最终阶段
                Debug.WriteLine($"判断是否最终阶段");
                if (PokeData.pokeFinalStageList.Contains(poke.nationalNumber))
                {
                    poke.ifFinalStage = true;
                }
                else
                {
                    poke.ifFinalStage = false;
                }

                // mega
                Debug.WriteLine($"判断是否mega");
                poke.ifMegaForm = false;

                // 判断是否传说
                Debug.WriteLine($"判断是否传说");
                if (PokeData.pokeLegendaryList.Contains(poke.nationalNumber))
                {
                    poke.ifLegendary = true;
                }
                else
                {
                    poke.ifLegendary = false;
                }

                // 增加至列表
                Debug.WriteLine($"增加至列表");
                pokeDatas.Add(poke);
            }

            // 更新mega数据
            List<string> pokeNameMegaList = [];

            foreach (var item in megaPokes)
            {
                var pokeName = "超级" + item[1].Trim().Split(' ')[0];
                if (!pokeNameMegaList.Contains(pokeName))
                {
                    pokeNameMegaList.Add(pokeName);
                }
                else
                {
                    continue;
                }

                var poke = new PokeData(pokeName);

                // 获取属性
                string[] temp_type = item[7].Split(':')[1].Split('/');
                foreach (var line in temp_type)
                {
                    if (line != "")
                    {
                        poke.type.Add(line.Trim());
                    }
                }

                // 获取特性
                string[] temp_abilities = item[8].Split(':')[1].Split('|');
                foreach (var line in temp_abilities)
                {
                    if (line != "")
                    {
                        var ability = line.Trim().Split(' ')[0];

                        if (poke.abilities.Count < 1 || !poke.abilities.Contains(ability))
                        {
                            poke.abilities.Add(ability);
                        }
                    }
                }

                // 获取baseStats
                string[] temp_baseStats = item[5].Split(':')[1].Trim().Split('.');
                for (int i = 0; i < temp_baseStats.Length; i++)
                {
                    try
                    {
                        //poke.baseStats[i] = Int16.Parse(temp_baseStats[i]);
                        poke.baseStats.Add(Int16.Parse(temp_baseStats[i]));
                    }
                    catch
                    {
                        MessageBox.Show("baseStats不是一个有效的值", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                // 获取进化阶段
                string stage = item[2].Split(':')[1].Trim();
                try
                {
                    poke.evolutionaryStage = Int16.Parse(stage);
                }
                catch
                {
                    MessageBox.Show("进化阶段不是一个有效的值", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // 查询全国图鉴编号
                GetNationalNumberAndNameDict(DBMegaTable, nationalNumberAndMegaNamesDict);

                foreach (var kvp in nationalNumberAndMegaNamesDict)
                {
                    if (pokeName == kvp.Value[7])
                    {
                        poke.nationalNumber = kvp.Key;
                    }
                }

                // 判断是否最终阶段
                if (PokeData.pokeFinalStageList.Contains(poke.nationalNumber))
                {
                    poke.ifFinalStage = true;
                }
                else
                {
                    poke.ifFinalStage = false;
                }

                // mega
                poke.ifMegaForm = true;

                // 判断是否传说
                if (PokeData.pokeLegendaryList.Contains(poke.nationalNumber))
                {
                    poke.ifLegendary = true;
                }
                else
                {
                    poke.ifLegendary = false;
                }

                // 增加至列表
                megaPokeDatas.Add(poke);
            }
        }

        // 主逻辑
        private async void BTN_Gen_Click(object sender, EventArgs e)
        {
            if (pokeNameFilePath == string.Empty || pokeDataFilePath == string.Empty)
            {
                return;
            }

            BTN_Gen.Enabled = false;

            startTime = DateTime.Now;
            timer.Start();

            // 删除已有的数据库
            try
            {
                DeleteDB();
            }
            catch
            {
                timer.Stop();
                CleanFormTitle();
                BTN_Gen.Enabled = true;
                MessageBox.Show("删除现有数据库失败,\n请确认没有被其他程序占用。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CreateTable();

            await ReadPokeNameFromFile();
            InsertPokeName(DBPokeTable, pokeNameList);

            if (pokeMegaNameFilePath != string.Empty)
            {
                await ReadMegaPokeNameFromFile();
                InsertPokeName(DBMegaTable, pokeMegaNameList);
            }

            InsertPokeDexNumber();

            UpdateMegaName();

            // 更新其他数据
            await ReadPokeDataFromFile();

            // 更新数据库
            UpdatePokeDatas(DBPokeTable, pokeDatas);
            UpdatePokeDatas(DBMegaTable, megaPokeDatas);

            BTN_Gen.Enabled = true;

            timer.Stop();
            CleanFormTitle();

            // 提示完成
            MessageBox.Show("运行完毕\n耗时：" + elapsedTime.ToString(@"mm\分ss\秒"), "提示", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        // 从网络生成
        private const string PokeWikiRootLink = "https://wiki.52poke.com";
        private const string PokeSumLink = "https://wiki.52poke.com/wiki/%E5%AE%9D%E5%8F%AF%E6%A2%A6%E5%88%97%E8%A1%A8%EF%BC%88%E6%8C%89%E5%85%A8%E5%9B%BD%E5%9B%BE%E9%89%B4%E7%BC%96%E5%8F%B7%EF%BC%89/%E7%AE%80%E5%8D%95%E7%89%88";
        private static List<string> pokeLinksList = [];
        private static List<PokeData> pokeStats = [];

        // 获取招式编号与名称（不获取其他信息）
        private const string PokeMoveListLink = "https://wiki.52poke.com/wiki/%E6%8B%9B%E5%BC%8F%E5%88%97%E8%A1%A8";
        private Dictionary<int, string> pokeMoveNameAndIndex = [];

        private string[] oldMoveName = ["诅咒", "鬼面", "鬼火", "空气斩", "吸血", "地狱突刺", "起死回生", "炼狱"];
        private string[] newMoveName = ["咒术", "可怕面孔", "磷火", "空气之刃", "汲取", "深渊突刺", "绝处逢生", "烈火深渊"];

        private List<string> unMatchingMoveName = [];

        // 进化相关
        private List<List<List<int>>> evolutionLinkList = [];
        private List<List<List<string>>> evolutionRuleLinkList = [];

        private async Task GetPokeMoveNameAndIndex()
        {
            try
            {
                pokeMoveNameAndIndex.Clear();
                using (var httpClient = new HttpClient())
                {
                    // 设置用户代理
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                    var html = await httpClient.GetStringAsync(PokeMoveListLink);
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(html);

                    // 选择所有的招式表
                    var tables = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'click-toggle')]").Elements("table").ToList();

                    if (tables != null && tables.Count > 0)
                    {
                        tables.RemoveAt(0);
                        int _count = 0;
                        foreach (var table in tables)
                        {
                            var rows = table.Element("tbody").Elements("tr");
                            foreach (var row in rows)
                            {
                                try
                                {
                                    if (!row.InnerHtml.Contains("td"))
                                    {
                                        continue;
                                    }

                                    var _index = row.Element("td").InnerText.Trim();
                                    var _success = int.TryParse(_index, out var index);

                                    if (_success)
                                    {
                                        _count++;

                                        var moveName = row.Elements("td").ToList()[1].InnerText.Trim();
                                        moveName = ReplaceOldMoveNameWithNew(HantToHans(moveName));

                                        pokeMoveNameAndIndex.Add(index, moveName);
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }

                        Debug.WriteLine($"共采集招式{_count}个");
                    }
                    else
                    {
                        Debug.WriteLine("当前阶段：[获取招式名称和序号]，没有找到任何元素。");
                        TB_Info.AppendText("当前阶段：[获取招式名称和序号]，没有找到任何元素。\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                TB_Info.AppendText($"当前阶段：[获取招式名称和序号]，发生错误: {ex.Message}\r\n");
                MessageBox.Show($"当前阶段：[获取招式名称和序号]，发生错误: {ex.Message}");
            }
        }

        private string ReplaceOldMoveNameWithNew(string name)
        {
            for (int i = 0; i < oldMoveName.Length; i++)
            {
                if (oldMoveName[i] == name)
                {
                    name = newMoveName[i];
                    return name;
                }
            }

            return name;
        }

        private string HantToHans(string input)
        {
            ZhConverter.Initialize();
            return input.ToHansFromHant();
        }

        private int GetMoveIndexFromName(string name)
        {
            foreach (var item in pokeMoveNameAndIndex)
            {
                if (item.Value == name)
                {
                    return item.Key;
                }
            }

            PrintColorfulDebugMessage($"没有查询到[{name}]的序号，请检查招式名称。", CustomColor.Magenta);
            return 0;
        }

        private enum CustomColor
        {
            Orange,
            Lime,
            Magenta,
            Gold
        }

        private void PrintColorfulDebugMessage(string input, CustomColor color)
        {
            if (input == null || input == string.Empty)
            {
                return;
            }

            var prefix = "[" + color + "]";

            Debug.WriteLine(prefix + input);
        }

        private async Task GetPokeLinks()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    // 设置用户代理
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                    var html = await httpClient.GetStringAsync(PokeSumLink);
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(html);

                    // 选择所有的tr元素
                    var rows = htmlDoc.DocumentNode.SelectNodes("//tr");

                    if (rows != null)
                    {
                        int _count = 0;
                        foreach (var row in rows)
                        {
                            var cells = row.SelectNodes("td");
                            if (cells != null && cells.Count >= 3)
                            {
                                _count++;
                                var firstCell = cells[0].InnerText.Trim();
                                var secondCell = cells[1].InnerText.Trim();

                                var linkNode = cells[1].SelectSingleNode(".//a");
                                var link = linkNode != null ? PokeWikiRootLink + linkNode.GetAttributeValue("href", string.Empty) : string.Empty;

                                pokeLinksList.Add(link);

                                Debug.WriteLine($"全国图鉴号: {firstCell}, 名称: {secondCell}, 链接: {link}");
                                TB_Info.AppendText($"全国图鉴号: {firstCell}, 名称: {secondCell}, 链接: {link}\r\n");

                                // debug
                                if (_count >= 18)
                                {
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("当前阶段：[获取全部链接]，没有找到任何tr元素。");
                        TB_Info.AppendText("当前阶段：[获取全部链接]，没有找到任何tr元素。\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                TB_Info.AppendText($"当前阶段：[获取全部链接]，发生错误: {ex.Message}\r\n");
                MessageBox.Show($"当前阶段：[获取全部链接]，发生错误: {ex.Message}");
            }
        }

        private static string RemoveParentheses(string input)
        {
            string pattern = @"[（）()]";
            return Regex.Replace(input, pattern, string.Empty);
        }

        private static string RemoveParenthesesAndInside(string input)
        {
            string pattern = @"[（(].*[）)]";
            return Regex.Replace(input, pattern, string.Empty);
        }

        private static string ExtractNumber(string input)
        {
            string pattern = @"\d+(\.\d+)?";
            Match match = Regex.Match(input, pattern);
            return match.Value;
        }

        private static string ExtractHatchTime(string input)
        {
            string pattern = @"\d+";
            Match match = Regex.Match(input, pattern);
            return match.Value;
        }

        private static string ExtractTailNumber(string input)
        {
            string pattern = @"\d*\z";
            Match match = Regex.Match(input, pattern);
            return match.Value;
        }

        private static bool CheckIfNumber(string input)
        {
            string pattern = @"^\d+$";
            return Regex.IsMatch(input, pattern);
        }

        private static void TestPattern()
        {
            var input = "21";
            var output = CheckIfNumber(input);
            Debug.WriteLine($"原始：{input}\n处理：{output}");
        }

        private static string GetPokedexDescription(HtmlNode node)
        {
            var tables = node.Element("tbody").Elements("tr").ToList()[1].Element("td").Elements("table").ToList();
            var count = tables.Count;
            var index = (int)Math.Floor((decimal)count / 2);
            var targetTable = tables[index];
            var description = targetTable.Element("tbody").Elements("tr").ToList()[1].Elements("td").Last().InnerText.Trim();

            if (description == "{{{scdex}}}" || description == string.Empty)
            {
                var td = targetTable.Element("tbody").Elements("tr").First().Elements("td").ToList();
                if (td.Count > 1)
                {
                    description = td[1].InnerText.Trim();

                    if (description == "{{{scdex}}}")
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return description;
                    }
                }
                else
                {
                    td = targetTable.Element("tbody").Elements("tr").ToList()[1].Elements("td").ToList();
                    if (td.Count > 1)
                    {
                        description = td[1].InnerText.Trim();

                        if (description == "{{{scdex}}}")
                        {
                            return string.Empty;
                        }
                        else
                        {
                            return description;
                        }
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            else
            {
                return description;
            }
        }

        private static string ReplaceFullWidthNumberWithHalfWidth(string input)
        {
            char[] chars = input.ToCharArray();
            string output = string.Empty;
            foreach (var item in chars)
            {
                switch (item)
                {
                    case '０':
                        output += '0';
                        break;
                    case '１':
                        output += '1';
                        break;
                    case '２':
                        output += '2';
                        break;
                    case '３':
                        output += '3';
                        break;
                    case '４':
                        output += '4';
                        break;
                    case '５':
                        output += '5';
                        break;
                    case '６':
                        output += '6';
                        break;
                    case '７':
                        output += '7';
                        break;
                    case '８':
                        output += '8';
                        break;
                    case '９':
                        output += '9';
                        break;
                    default:
                        output += item;
                        break;
                }
            }

            return output;
        }

        private async Task GetPokeBasicStats()
        {
            pokeDatas.Clear();
            unMatchingMoveName.Clear();
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                    int _count = 0;
                    foreach (var pokeLink in pokeLinksList)
                    {
                        _count++;

                        // 随机延迟
                        bool shouldDelay = false;
                        byte[] buffer = Guid.NewGuid().ToByteArray();
                        int iSeed = BitConverter.ToInt32(buffer, 0);
                        Random random = new Random(iSeed);
                        int delay = random.Next(50, 1050);
                        if (shouldDelay)
                        {
                            Debug.WriteLine($"当前阶段：[获取基础信息]，即将休眠：{delay} 毫秒");
                            Thread.Sleep(delay);
                        }

                        var html = await httpClient.GetStringAsync(pokeLink);
                        var htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(html);

                        // 获取所有必要信息，然后存储到pokedata对象
                        // 获取基本节点
                        var basicStatsTable = htmlDoc.DocumentNode.SelectNodes("//table[contains(@class, 'roundy')]")[2];

                        if (basicStatsTable != null)
                        {
                            // 获取基础信息
                            var trRows = basicStatsTable.Element("tbody").Elements("tr").ToList();

                            // 名称
                            var row1 = trRows[0];
                            var name = row1.Descendants("b").First().InnerText;

                            // 全国图鉴号
                            var _pokedexNumber = row1.Descendants("th").First().InnerText.Replace('#', '0');
                            int.TryParse(_pokedexNumber, out int pokedexNumber);

                            // 属性
                            List<string> type = [];
                            var row3 = trRows[2];
                            var types = row3.Descendants("a").ToList();
                            type.Add(types[1].InnerText);
                            if (!types[2].InnerText.Contains("分类"))
                            {
                                type.Add(types[2].InnerText);
                            }

                            // 分类
                            var row3td2 = row3.Elements("td").Last();
                            var category = row3td2.Descendants("a").ToList().Last().InnerText;

                            // 特性
                            List<string> abilities = [];
                            List<string> hiddenAbilities = [];
                            bool hiddenAbilitiesExist = true;
                            var row4 = trRows[3];
                            var abilityNodes = row4.Descendants("tr").Last().Elements("td").ToList();

                            if (abilityNodes.Count > 1)
                            {
                                // 普通特性
                                foreach (var _ability in abilityNodes[0].Elements("a"))
                                {
                                    abilities.Add(_ability.InnerText);
                                }

                                // 隐藏特性
                                foreach (var _ability in abilityNodes[1].Elements("a"))
                                {
                                    hiddenAbilities.Add(_ability.InnerText);
                                }
                            }
                            else
                            {
                                hiddenAbilitiesExist = false;

                                var _ability = row4.Element("td").Descendants("a").Last().InnerText;
                                abilities.Add(_ability);
                            }

                            // 其他数据
                            string levelingRate, shape, pokedexColor;
                            float height, weight;
                            int catchRate, hatchTime;
                            List<float> genderRatio = [];
                            List<string> eggGroups = [];
                            List<int> EVYield = [];

                            if (hiddenAbilitiesExist)
                            {
                                // 经验增长速度
                                var row5 = trRows[4];
                                var _levelingRate = row5.Descendants("small").ToList().First().InnerText;
                                levelingRate = RemoveParentheses(_levelingRate);

                                // 身高、体重
                                var row8 = trRows[7];
                                var _height = row8.Elements("td").ToList()[0].Descendants("td").ToList()[1].InnerText.Trim();
                                var _weight = row8.Elements("td").ToList()[1].Descendants("td").ToList()[1].InnerText.Trim();
                                float.TryParse(_height.Replace("m", ""), out height);
                                float.TryParse(_weight.Replace("kg", ""), out weight);

                                // 体型
                                var row9 = trRows[8];
                                shape = row9.Descendants("img").First().GetAttributeValue("alt", "");

                                // 图鉴颜色、捕获率
                                var row10 = trRows[9];
                                pokedexColor = row10.Elements("td").ToList()[0].Descendants("span").First().InnerText;
                                var _catchRate = row10.Elements("td").ToList()[1].Descendants("td").Last().InnerText;
                                _catchRate = RemoveParenthesesAndInside(_catchRate);
                                int.TryParse(_catchRate, out catchRate);

                                // 性别比例
                                var row11 = trRows[10];
                                var _genderRatio = row11.Descendants("span").First().InnerText;
                                float.TryParse(ExtractNumber(_genderRatio), out var maleRatio);
                                var femaleRatio = 100 - maleRatio;
                                genderRatio.Add(maleRatio);
                                genderRatio.Add(femaleRatio);

                                // 蛋群、孵化周期
                                var row12 = trRows[11];
                                var row12tr2 = row12.Descendants("tr").Last().Descendants("td").ToList();
                                var _eggGroupsNode = row12tr2[0].Descendants("a").ToList();
                                foreach (var node in _eggGroupsNode)
                                {
                                    eggGroups.Add(node.InnerText);
                                }
                                var _hatchTime = row12tr2[1].InnerText;
                                int.TryParse(ExtractHatchTime(_hatchTime), out hatchTime);

                                // 取得基础点数
                                var row13 = trRows[12];
                                var row13tr2 = row13.Descendants("tr").ToList()[1];
                                var _EVYield = row13tr2.Elements("td").ToList();
                                foreach (var node in _EVYield)
                                {
                                    var _ev = node.InnerText.Trim();
                                    int.TryParse(ExtractTailNumber(_ev), out var ev);
                                    EVYield.Add(ev);
                                }
                            }
                            else
                            {
                                // 经验增长速度
                                var _levelingRate = row4.Elements("td").ToList().Last().Descendants("small").ToList().First().InnerText;
                                levelingRate = RemoveParentheses(_levelingRate);

                                // 身高、体重
                                var row7 = trRows[6];
                                var _height = row7.Elements("td").ToList()[0].Descendants("td").ToList()[1].InnerText.Trim();
                                var _weight = row7.Elements("td").ToList()[1].Descendants("td").ToList()[1].InnerText.Trim();
                                float.TryParse(_height.Replace("m", ""), out height);
                                float.TryParse(_weight.Replace("kg", ""), out weight);

                                // 体型
                                var row8 = trRows[7];
                                shape = row8.Descendants("img").First().GetAttributeValue("alt", "");

                                // 图鉴颜色、捕获率
                                var row9 = trRows[8];
                                pokedexColor = row9.Elements("td").ToList()[0].Descendants("span").First().InnerText;
                                var _catchRate = row9.Elements("td").ToList()[1].Descendants("td").Last().InnerText;
                                _catchRate = RemoveParenthesesAndInside(_catchRate);
                                int.TryParse(_catchRate, out catchRate);

                                // 性别比例
                                var row10 = trRows[9];
                                var _genderRatio = row10.Descendants("span").First().InnerText;
                                float.TryParse(ExtractNumber(_genderRatio), out var maleRatio);
                                var femaleRatio = 100 - maleRatio;
                                genderRatio.Add(maleRatio);
                                genderRatio.Add(femaleRatio);

                                // 蛋群、孵化周期
                                var row11 = trRows[10];
                                var row11tr2 = row11.Descendants("tr").Last().Descendants("td").ToList();
                                var _eggGroupsNode = row11tr2[0].Descendants("a").ToList();
                                foreach (var node in _eggGroupsNode)
                                {
                                    eggGroups.Add(node.InnerText);
                                }
                                var _hatchTime = row11tr2[1].InnerText;
                                int.TryParse(ExtractHatchTime(_hatchTime), out hatchTime);

                                // 取得基础点数
                                var row12 = trRows[11];
                                var row12tr2 = row12.Descendants("tr").ToList()[1];
                                var _EVYield = row12tr2.Elements("td").ToList();
                                foreach (var node in _EVYield)
                                {
                                    var _ev = node.InnerText.Trim();
                                    int.TryParse(ExtractTailNumber(_ev), out var ev);
                                    EVYield.Add(ev);
                                }
                            }

                            List<int> baseStats = [];
                            string pokedexDescription;
                            List<int> learnsetLevelingUp = [];
                            List<int> learnsetLevelingUpLevel = [];
                            List<int> learnsetTM = [];

                            // 种族值
                            var rowHP = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'bgl-HP')]").First();
                            var _HP = rowHP.Element("th").Descendants("span").Last().InnerText;
                            int.TryParse(_HP, out var HP);
                            baseStats.Add(HP);

                            var rowATK = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'bgl-攻击')]").First();
                            var _ATK = rowATK.Element("th").Descendants("span").Last().InnerText;
                            int.TryParse(_ATK, out var ATK);
                            baseStats.Add(ATK);

                            var rowDEF = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'bgl-防御')]").First();
                            var _DEF = rowDEF.Element("th").Descendants("span").Last().InnerText;
                            int.TryParse(_DEF, out var DEF);
                            baseStats.Add(DEF);

                            var rowSPA = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'bgl-特攻')]").First();
                            var _SPA = rowSPA.Element("th").Descendants("span").Last().InnerText;
                            int.TryParse(_SPA, out var SPA);
                            baseStats.Add(SPA);

                            var rowSPD = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'bgl-特防')]").First();
                            var _SPD = rowSPD.Element("th").Descendants("span").Last().InnerText;
                            int.TryParse(_SPD, out var SPD);
                            baseStats.Add(SPD);

                            var rowSPE = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'bgl-速度')]").First();
                            var _SPE = rowSPE.Element("th").Descendants("span").Last().InnerText;
                            int.TryParse(_SPE, out var SPE);
                            baseStats.Add(SPE);

                            // 图鉴描述
                            var descriptionTable = htmlDoc.DocumentNode.SelectNodes("//table[contains(@class, 'a-c at-c roundy')]").Last();
                            if (descriptionTable != null)
                            {
                                var availableDescTable = descriptionTable.Element("tbody").Elements("tr").Last().Element("td").Elements("table").ToList();
                                availableDescTable.RemoveAt(availableDescTable.Count - 1);
                                var targetTable = availableDescTable.Last();
                                var description = GetPokedexDescription(targetTable);

                                while (description == string.Empty && availableDescTable.Count > 1)
                                {
                                    availableDescTable.RemoveAt(availableDescTable.Count - 1);
                                    targetTable = availableDescTable.Last();
                                    description = GetPokedexDescription(targetTable);
                                }

                                pokedexDescription = description;
                            }
                            else
                            {
                                Debug.WriteLine("descriptionTable为空");
                                pokedexDescription = string.Empty;
                            }

                            // 可学会的招式
                            var learnsetLevelingUpTable = htmlDoc.DocumentNode.SelectNodes("//table[contains(@class, 'a-c at-c sortable')]").First();
                            var trLevelingUp = learnsetLevelingUpTable.Element("tbody").Elements("tr").ToList();

                            trLevelingUp.RemoveAt(0);
                            trLevelingUp.RemoveAt(0);
                            trLevelingUp.RemoveAt(trLevelingUp.Count - 1);

                            foreach (var node in trLevelingUp)
                            {
                                var td = node.Elements("td").ToList();
                                var _level = td.First().InnerText.Trim();

                                if (!CheckIfNumber(_level))
                                {
                                    _level = "1";
                                }
                                int.TryParse(_level, out int level);

                                var move = td[2].Descendants("a").First().InnerText.Trim();
                                move = ReplaceOldMoveNameWithNew(HantToHans(move));

                                var index = GetMoveIndexFromName(move);

                                learnsetLevelingUp.Add(index);
                                learnsetLevelingUpLevel.Add(level);
                            }

                            // 能使用的招式学习器
                            var learnsetTMTable = htmlDoc.DocumentNode.SelectNodes("//table[contains(@class, 'a-c at-c sortable')]").ToList()[1];
                            var trTM = learnsetTMTable.Element("tbody").Elements("tr").ToList();

                            if (trTM.Count > 3)
                            {
                                trTM.RemoveAt(0);
                                trTM.RemoveAt(0);
                                trTM.RemoveAt(trTM.Count - 1);

                                foreach (var node in trTM)
                                {
                                    var td = node.Elements("td").ToList();

                                    if (td.Count > 1)
                                    {
                                        var _TM = td[2].Descendants("a").First().InnerText.Trim();
                                        _TM = ReplaceOldMoveNameWithNew(HantToHans(_TM));

                                        var moveIndex = GetMoveIndexFromName(_TM);
                                        AddToListIfNotExist<int>(moveIndex, learnsetTM);

                                        if (moveIndex == 0)
                                        {
                                            AddToListIfNotExist<string>(_TM, unMatchingMoveName);
                                        }
                                    }
                                }

                                PokeData.QuickSort(learnsetTM, 0, learnsetTM.Count - 1);
                            }

                            // PokeData建立
                            var poke = new PokeData(pokedexNumber);
                            poke.name = name;
                            poke.category = category;
                            poke.levelingRate = levelingRate;
                            poke.height = height;
                            poke.weight = weight;
                            poke.shape = shape;
                            poke.pokedexColor = pokedexColor;
                            poke.catchRate = catchRate;
                            poke.hatchTime = hatchTime;
                            poke.pokedexDescription = pokedexDescription;
                            poke.type = type;
                            poke.abilities = abilities;
                            poke.hiddenAbilities = hiddenAbilities;
                            poke.genderRatio = genderRatio;
                            poke.eggGroups = eggGroups;
                            poke.EVYield = EVYield;
                            poke.baseStats = baseStats;
                            poke.learnsetLevelingUp = learnsetLevelingUp;
                            poke.learnsetLevelingUPLevel = learnsetLevelingUpLevel;
                            poke.learnsetTM = learnsetTM;
                            //未完成
                            poke.evolutionaryStage = 0;

                            pokeDatas.Add(poke);

                            // debug
                            string typeOutput = string.Join(", ", type);
                            string abilitiesOutput = string.Join(", ", abilities);
                            string hiddenAbilitiesOutput = string.Join(", ", hiddenAbilities);
                            string genderRatioOutput = string.Join(", ", genderRatio);
                            string eggGroupsOutput = string.Join(", ", eggGroups);
                            string EVYieldOutput = string.Join(", ", EVYield);
                            string baseStatsOutput = string.Join(", ", baseStats);
                            string learnsetLevelingUpOutput = string.Join(", ", learnsetLevelingUp);
                            string learnsetTMOutput = string.Join(", ", learnsetTM);

                            var outputInfo = $"{pokedexNumber}-{name}-{typeOutput}-{category}-普通特性:{abilitiesOutput}-隐藏特性:{hiddenAbilitiesOutput}-经验增长速度:{levelingRate}" +
                                $"-身高:{height}米-体重:{weight}千克-体型:{shape}-图鉴颜色:{pokedexColor}-捕获率:{catchRate}-性别比例:{genderRatioOutput}-蛋群:{eggGroupsOutput}" +
                                $"-孵化周期:{hatchTime}-取得基础点数:{EVYieldOutput}-种族值:{baseStatsOutput}-图鉴描述:{pokedexDescription}-可学会的招式:{learnsetLevelingUpOutput}" +
                                $"-能使用的招式学习器:{learnsetTMOutput}";
                            Debug.WriteLine(outputInfo);
                            TB_Info.AppendText(outputInfo + "\r\n");
                            Debug.WriteLine($"↑当前阶段：[获取基础信息]，已处理条目：{_count} / {pokeLinksList.Count}");
                            TB_Info.AppendText($"当前阶段：[获取基础信息]，已处理条目：{_count} / {pokeLinksList.Count}\r\n");
                        }
                        else
                        {
                            Debug.WriteLine($"↑当前阶段：[获取基础信息]，没有找到任何匹配元素，当前条目：{_count}");
                            TB_Info.AppendText($"当前阶段：[获取基础信息]，没有找到任何匹配元素，当前条目：{_count}\r\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TB_Info.AppendText($"当前阶段：[获取基础信息]，发生错误: {ex.Message}\r\n");
                MessageBox.Show($"当前阶段：[获取基础信息]，发生错误: {ex.Message}");
            }
        }

        private void AddToListIfNotExist<T>(T newItem, List<T> list)
        {
            if (newItem == null)
            {
                return;
            }

            if (list.Count == 0)
            {
                list.Add(newItem);
                return;
            }

            foreach (var item in list)
            {
                if (item == null)
                {
                    continue;
                }

                if (item.Equals(newItem))
                {
                    return;
                }
            }

            list.Add(newItem);
        }

        private async Task SaveToDesktop(string content, string fileName)
        {
            fileName += ".txt";
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            path = Path.Combine(path, fileName);
            Console.WriteLine(path);
            await using var streamWriter = new StreamWriter(path);
            await streamWriter.WriteAsync(content);
            streamWriter.Close();
        }

        private async Task<string> ReadEvolutionTree()
        {
            try
            {
                var inputStream = new StreamReader("data/进化树.txt", Encoding.UTF8);
                var input = await inputStream.ReadToEndAsync();
                inputStream.Close();

                return input;
            }
            catch
            {
                PrintColorfulDebugMessage("读取文件[进化树.txt]错误，请检查文件是否存在!", CustomColor.Magenta);
                return string.Empty;
            }
        }

        private List<string> SplitToLine(string input)
        {
            List<string> list = [];
            string[] inputString = input.Split(['\r', '\n']);
            foreach (var line in inputString)
            {
                if (line != "")
                {
                    list.Add(line.Trim(['\t', ' ']));
                }
            }

            return list;
        }

        private void SeparateEvolutionAndRule(List<string> list, out List<List<string>> evolutionLink, out List<List<string>> evolutionRuleLink)
        {
            evolutionLink = [];
            evolutionRuleLink = [];
            foreach (var line in list)
            {
                List<string> singleEvolutionLink = [];
                List<string> singleRuleLink = [];
                string[] temp = line.Split(",");
                for (int i = 0; i < temp.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        singleEvolutionLink.Add(temp[i]);
                    }
                    else
                    {
                        singleRuleLink.Add(temp[i]);
                    }
                }
                evolutionLink.Add(singleEvolutionLink);
                evolutionRuleLink.Add(singleRuleLink);
            }
        }

        private void PrintList<T>(List<T> list)
        {
            if (list == null || list.Count < 1)
            {
                return;
            }

            var output = string.Join(", ", list);
            PrintColorfulDebugMessage(output, CustomColor.Lime);
        }

        private void PrintList<T>(List<List<T>> list)
        {
            if (list == null || list.Count < 1)
            {
                return;
            }

            foreach (var item in list)
            {
                PrintList(item);
            }
        }

        private List<List<int>> ReplacePokeNameWithNationalNumber(List<List<string>> rawEvolutionLink)
        {
            // get poke and nationalnumber
            Dictionary<string, int> nationalNumberAndNameDict = [];
            foreach (var poke in pokeDatas)
            {
                var name = poke.name;
                var nationalNumber = poke.nationalNumber;
                nationalNumberAndNameDict.Add(name, nationalNumber);
                //Debug.WriteLine($"{nationalNumber}-{name}");
            }

            List<List<int>> newEvolutionLink = [];
            foreach (var evolutionLink in rawEvolutionLink)
            {
                List<int> _evolutionLink = [];
                foreach (var name in evolutionLink)
                {
                    bool exist = false;
                    foreach (var line in nationalNumberAndNameDict)
                    {
                        if (name == line.Key)
                        {
                            exist = true;
                            _evolutionLink.Add(line.Value);
                            continue;
                        }
                    }

                    if (!exist)
                    {
                        PrintColorfulDebugMessage($"替换名称为图鉴号时失败:{name}", CustomColor.Magenta);
                        _evolutionLink.Add(0);
                    }
                }
                newEvolutionLink.Add(_evolutionLink);
            }

            return newEvolutionLink;
        }

        private void MergeEvolutionLinkAndRule(List<List<int>> evolutionLink, List<List<string>> evolutionRuleLink)
        {
            evolutionLinkList.Clear();
            evolutionRuleLinkList.Clear();

            Dictionary<int, List<int>> evolutionDict = [];
            for (int i = 0; i < evolutionLink.Count; i++)
            {
                if (!evolutionDict.ContainsKey(evolutionLink[i][0]))
                {
                    List<List<int>> eList = [];
                    eList.Add(evolutionLink[i]);
                    evolutionLinkList.Add(eList);

                    List<List<string>> rList = [];
                    rList.Add(evolutionRuleLink[i]);
                    evolutionRuleLinkList.Add(rList);

                    evolutionDict.Add(evolutionLink[i][0], evolutionLink[i]);
                }
                else
                {
                    List<List<List<int>>> copyLink = evolutionLinkList.Select(innerList => innerList.Select(innerInnerList => new List<int>(innerInnerList)).ToList()).ToList();
                    for (int j = 0; j < copyLink.Count; j++)
                    {
                        foreach (var link in copyLink[j])
                        {
                            if (link[0] == evolutionLink[i][0])
                            {
                                evolutionLinkList[j].Add(evolutionLink[i]);
                                evolutionRuleLinkList[j].Add(evolutionRuleLink[i]);
                            }
                        }
                    }
                }
            }
        }

        private async Task ProcessEvolutionTree()
        {
            List<List<string>> rawEvolutionLink = [];
            List<List<string>> rawEvolutionRuleLink = [];
            SeparateEvolutionAndRule(SplitToLine(await ReadEvolutionTree()), out rawEvolutionLink, out rawEvolutionRuleLink);

            // 分别存储原始进化链和进化规则链到DB新表

            // debug
            //PrintRawEvolutionLinkAndRule(rawEvolutionLink, rawEvolutionRuleLink);
            PrintColorfulDebugMessage($"rawEvolutionLink长度：{rawEvolutionLink.Count}, rawEvolutionRuleLink长度：{rawEvolutionRuleLink.Count}", CustomColor.Gold);

            // 合并同家族进化链
            List<List<int>> _evolutionLink = ReplacePokeNameWithNationalNumber(rawEvolutionLink);
            MergeEvolutionLinkAndRule(_evolutionLink, rawEvolutionRuleLink);
        }

        private void PrintRawEvolutionLinkAndRule(List<List<string>> rawEvolutionLink, List<List<string>> rawEvolutionRuleLink)
        {
            PrintColorfulDebugMessage("进化链:", CustomColor.Orange);
            PrintList(rawEvolutionLink);
            PrintColorfulDebugMessage("进化规则链:", CustomColor.Orange);
            PrintList(rawEvolutionRuleLink);
        }

        private void PrintEvolutionLinkAndRule(List<List<int>> evolutionLink, List<List<string>> evolutionRuleLink)
        {
            PrintColorfulDebugMessage("进化链:", CustomColor.Orange);
            PrintList(evolutionLink);
            PrintColorfulDebugMessage("进化规则链:", CustomColor.Orange);
            PrintList(evolutionRuleLink);
        }

        private static bool firstRun = true;
        private async Task CollectOnlineContent()
        {
            firstRun = false;

            await GetPokeMoveNameAndIndex();
            await GetPokeLinks();
            await GetPokeBasicStats();
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            TB_Info.Clear();
            TB_Info.AppendText($"开始运行，请耐心等待...\r\n");

            if (firstRun)
            {
                await CollectOnlineContent();

                string unMatchingMoveNameText = string.Join(Environment.NewLine, unMatchingMoveName);
#if DEBUG
                await SaveToDesktop(unMatchingMoveNameText, "unMatchingMoveName");
#endif
            }

            try
            {
                await ProcessEvolutionTree();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return;
            }

            PrintColorfulDebugMessage($"evolutionLinkList长度：{evolutionLinkList.Count.ToString()}", CustomColor.Gold);
            PrintColorfulDebugMessage($"evolutionRuleLinkList长度：{evolutionRuleLinkList.Count.ToString()}", CustomColor.Gold);
            PrintEvolutionLinkAndRule(evolutionLinkList[0], evolutionRuleLinkList[0]);
            PrintEvolutionLinkAndRule(evolutionLinkList[1], evolutionRuleLinkList[1]);
            PrintEvolutionLinkAndRule(evolutionLinkList[2], evolutionRuleLinkList[2]);
            PrintEvolutionLinkAndRule(evolutionLinkList[3], evolutionRuleLinkList[3]);
            PrintEvolutionLinkAndRule(evolutionLinkList[4], evolutionRuleLinkList[4]);
            PrintEvolutionLinkAndRule(evolutionLinkList[5], evolutionRuleLinkList[5]);

            //TestPattern();

            TB_Info.AppendText($"运行结束。\r\n");
        }
    }
}
