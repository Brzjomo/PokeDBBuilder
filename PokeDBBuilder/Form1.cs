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
                    "BST TEXT, " +
                    "evolutionaryStage INTEGER, " +
                    "evolutionFamily TEXT, " +
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
                var BST = string.Join(",", Array.ConvertAll(poke.BST, x => x.ToString()));
                var evolutionaryStage = poke.evolutionaryStage.ToString();
                var ifFinalStage = poke.ifFinalStage.ToString();
                var ifMegaForm = poke.ifMegaForm.ToString();
                var ifLegendary = poke.ifLegendary.ToString();

                string query = $"UPDATE {DBTable} SET type = '{type}', abilities = '{abilities}', BST = '{BST}', evolutionaryStage = '{evolutionaryStage}'," +
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
                } else
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

            foreach (var item in normalPokes)
            {
                var pokeName = item[1].Trim();
                if (!pokeNameList.Contains(pokeName))
                {
                    pokeNameList.Add(pokeName);
                } else
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

                foreach (var kvp in nationalNumberAndNamesDict)
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
                poke.ifMegaForm = false;

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

        // 临时处理元数据用
        string filePath = @"C:\Users\brzjomo\Downloads\6-29.txt";
        private async Task ProcessNumberList()
        {
            // 读取源文件
            var inputStream = new StreamReader(filePath, Encoding.UTF8);
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
                    if (line.Contains('\t') && !sourceList.Contains(line.Trim(['\t'])))
                    {
                        sourceList.Add(line.Trim(['\t']));
                    }
                    else if (!sourceList.Contains(line))
                    {
                        sourceList.Add(line);
                    }
                }
            }
            string output = string.Join(", ", sourceList.Select(num => Int16.Parse(num)));
            Clipboard.SetText(output);

            MessageBox.Show("运行完毕", "提示", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }
}
