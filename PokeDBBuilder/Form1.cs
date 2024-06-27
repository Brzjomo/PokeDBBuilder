using System.Data.SQLite;
using System.Text;
using Application = System.Windows.Forms.Application;

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

        private static List<string> pokeNameList = [];
        private static List<string> pokeMegaNameList = [];

        Dictionary<int, string[]> nationalNumberAndNamesDict = [];
        Dictionary<int, string[]> nationalNumberAndMegaNamesDict = [];
        private static List<PokeData> pokeDatas = [];

        public Form1()
        {
            InitializeComponent();
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
                    "BST TEXT, " +
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
                    MessageBox.Show(pokeName, "Debug", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                StringBuilder sb = new();
                foreach (var item in PokeData.getMegaList())
                {
                    sb.Append(item.ToString());
                    sb.Append(", ");
                }
                if (sb.Length > 0)
                {
                    sb.Length -= 2;
                }

                var query = "INSERT INTO " + DBMegaTable + " SELECT * FROM " + DBPokeTable + " WHERE nationalNumber IN (" + sb.ToString() + ")";
                var copyMega = new SQLiteCommand(query, connection);
                try
                {
                    copyMega.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(query + "\n" + ex.Message, "Debug", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        //MessageBox.Show(name, "Debug", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        string updatedName = UpdateName(name);
                        //MessageBox.Show(updatedName, "Debug", MessageBoxButtons.OK, MessageBoxIcon.Error);

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
                } else if (i == 8)
                {
                    elements[i] = "超級" + elements[i];
                } else
                {
                    elements[i] = elements[i] + " Mega";
                }
            }
            return string.Join(",", elements);
        }

        private void GetNationalNumberAndNameDict(string DBtable, Dictionary<int, string[]> dict)
        {
            dict.Clear();

            var connection = CreateSQLiteConnection();
            connection.Open();

            string query = $"SELECT nationalNumber, name FROM {DBtable}";
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

            List<string> pokeDataNameList = [];
            List<string> pokeDataBlackList = ["岩狗狗 1", "蝶结萌虻 1", "托戈德玛尔 1", "滴蛛霸 1", "奈克洛兹玛 3", "奈克洛兹玛 2", "奈克洛兹玛 1", "杖尾鳞甲龙 1", "焰后蜥 1", "兰螳花 1", "锹农炮虫 1", "猫鼬探长 1",
                "皮卡丘 7", "皮卡丘 6", "皮卡丘 5", "皮卡丘 4", "皮卡丘 3", "皮卡丘 2", "皮卡丘 1", "玛机雅娜 1", "谜拟丘 3", "谜拟丘 2", "谜拟丘 1", "三地鼠 1", "地鼠 1", "小陨星 13", "小陨星 12", "小陨星 11",
                "小陨星 10", "小陨星 9", "小陨星 8", "小陨星 7", "小陨星 6", "小陨星 5", "小陨星 4", "小陨星 3", "小陨星 2", "小陨星 1", "基格尔德 4", "基格尔德 3", "基格尔德 2", "基格尔德 1", "甲贺忍蛙 2",
                "甲贺忍蛙 1", "嘎啦嘎啦 2", "嘎啦嘎啦 1", "椰蛋树 1", "臭臭泥 1", "臭泥 1", "隆隆岩 1", "隆隆石 1", "小拳石 1", "猫老大 1", "喵喵 1", "九尾 1", "六尾 1", "穿山王 1", "穿山鼠 1", "雷丘 1", "拉达 2",
                "拉达 1", "小拉达 1", "鬃岩狼人 2", "鬃岩狼人 1", "花舞鸟 3", "花舞鸟 2", "花舞鸟 1", "弱丁鱼 1", "花叶蒂 5", "花叶蒂 4", "花叶蒂 3", "花叶蒂 2", "花叶蒂 1", "南瓜怪人 3", "南瓜怪人 2", "南瓜怪人 1",
                "南瓜精 3", "南瓜精 2", "南瓜精 1", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""];

            // 处理数据
            foreach (var item in pokeDataLists)
            {
                var pokeName = item[1].Trim();

                // 更改Mega名称
                bool ifMegaForm = pokeName.Contains('1') && !pokeDataBlackList.Contains(pokeName);

                if (ifMegaForm)
                {
                    pokeName = pokeName.Replace(" 1", "");
                    pokeName = "超级" + pokeName;
                }

                if (!pokeDataNameList.Contains(pokeName) && !pokeDataBlackList.Contains(pokeName))
                {
                    pokeDataNameList.Add(pokeName);
                } else
                {
                    continue;
                }

                var poke = new PokeData(pokeName);

                // 判断是否Mega
                if (ifMegaForm)
                {
                    poke.ifMegaForm = ifMegaForm;
                }

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

                // 获取BST
                string[] temp_BST = item[5].Split(':')[1].Trim().Split('.');
                for (int i = 0; i < temp_BST.Length; i++)
                {
                    try
                    {
                        poke.BST[i] = Int16.Parse(temp_BST[i]);
                    }
                    catch
                    {
                        MessageBox.Show("BST不是一个有效的值", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                GetNationalNumberAndNameDict(DBPokeTable, nationalNumberAndNamesDict);
                GetNationalNumberAndNameDict(DBMegaTable, nationalNumberAndMegaNamesDict);

                if (!ifMegaForm)
                {
                    foreach (var kvp in nationalNumberAndNamesDict)
                    {
                        if (pokeName == kvp.Value[7])
                        {
                            poke.nationalNumber = kvp.Key;
                        }
                    }
                } else
                {
                    foreach (var kvp in nationalNumberAndMegaNamesDict)
                    {
                        if (pokeName == kvp.Value[7])
                        {
                            poke.nationalNumber = kvp.Key;
                        }
                    }
                }

                // 判断是否最终阶段
                if (PokeData.pokeFinalStageList.Contains(poke.nationalNumber))
                {
                    poke.ifFinalStage = true;
                } else
                {
                    poke.ifFinalStage = false;
                }

                // 判断是否传说
                if (PokeData.pokeLegendaryList.Contains(poke.nationalNumber))
                {
                    poke.ifLegendary = true;
                }
                else
                {
                    poke.ifLegendary = false;
                }

                pokeDatas.Add(poke);
            }
        }

        // 主逻辑
        private async void BTN_Gen_Click(object sender, EventArgs e)
        {
            BTN_Gen.Enabled = false;

            if (pokeNameFilePath == string.Empty)
            {
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

            BTN_Gen.Enabled = true;

            // 提示完成
            MessageBox.Show("运行完毕", "提示", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }
}
