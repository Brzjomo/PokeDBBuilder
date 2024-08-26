using HtmlAgilityPack;
using System.Data.SQLite;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
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
            Text = "PokeDBBuilder" + "  �����У�" + elapsedTime.ToString(@"hh\:mm\:ss");
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
                // ������
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

            // ��������
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
                // ���Ƶ�Mega��
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

            // ��ѯname�е�ֵ���������ŷָ�
            string query = "SELECT name FROM " + DBMegaTable;
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader["name"].ToString();
                        string updatedName = UpdateName(name);

                        // ����name�е�ֵ
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
                if (i == 7) // ��8��Ԫ��
                {
                    elements[i] = "����" + elements[i];
                }
                else if (i == 8)
                {
                    elements[i] = "����" + elements[i];
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
                // ��filePath�е��ļ���·����ȡ����
                string[] fileName = pokeNameFilePath.Split("\\");
                string fileFolderPath = pokeNameFilePath.Replace(fileName[^1], "");

                // ���ļ���
                try
                {
                    System.Diagnostics.Process.Start("explorer", fileFolderPath);
                }
                catch
                {
                    MessageBox.Show("·��������ȷ��·���Ƿ���ڣ�", "·������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LLB_File_2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (pokeDataFilePath != "")
            {
                // ��filePath�е��ļ���·����ȡ����
                string[] fileName = pokeDataFilePath.Split("\\");
                string fileFolderPath = pokeDataFilePath.Replace(fileName[^1], "");

                // ���ļ���
                try
                {
                    System.Diagnostics.Process.Start("explorer", fileFolderPath);
                }
                catch
                {
                    MessageBox.Show("·��������ȷ��·���Ƿ���ڣ�", "·������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LLB_File_3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (pokeMegaNameFilePath != "")
            {
                // ��filePath�е��ļ���·����ȡ����
                string[] fileName = pokeMegaNameFilePath.Split("\\");
                string fileFolderPath = pokeMegaNameFilePath.Replace(fileName[^1], "");

                // ���ļ���
                try
                {
                    System.Diagnostics.Process.Start("explorer", fileFolderPath);
                }
                catch
                {
                    MessageBox.Show("·��������ȷ��·���Ƿ���ڣ�", "·������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BTN_Open_F1_Click(object sender, EventArgs e)
        {
            // ���ļ�
            OpenFileDialog openFileDialog = new()
            {
                FileName = "ѡ��һ��txt�ļ�",
                Filter = "txt�ļ�(*.txt)|*.txt",
                Title = "��txt�ļ�"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // �����ļ���
                pokeNameFilePath = openFileDialog.FileName;

                // �����ļ���
                string[] fileName = openFileDialog.FileName.Split("\\");
                string openFileName = fileName[^1];
                LLB_File_1.Text = openFileName;
            }
        }

        private void BTN_Open_F2_Click(object sender, EventArgs e)
        {
            // ���ļ�
            OpenFileDialog openFileDialog = new()
            {
                FileName = "ѡ��һ��txt�ļ�",
                Filter = "txt�ļ�(*.txt)|*.txt",
                Title = "��txt�ļ�"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // �����ļ���
                pokeDataFilePath = openFileDialog.FileName;

                // �����ļ���
                string[] fileName = openFileDialog.FileName.Split("\\");
                string openFileName = fileName[^1];
                LLB_File_2.Text = openFileName;
            }
        }

        private void BTN_Open_F3_Click(object sender, EventArgs e)
        {
            // ���ļ�
            OpenFileDialog openFileDialog = new()
            {
                FileName = "ѡ��һ��txt�ļ�",
                Filter = "txt�ļ�(*.txt)|*.txt",
                Title = "��txt�ļ�"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // �����ļ���
                pokeMegaNameFilePath = openFileDialog.FileName;

                // �����ļ���
                string[] fileName = openFileDialog.FileName.Split("\\");
                string openFileName = fileName[^1];
                LLB_File_3.Text = openFileName;
            }
        }

        // ��ȡ����
        private async Task ReadPokeNameFromFile()
        {
            pokeNameList.Clear();

            // ��ȡԴ�ļ�
            var inputStream = new StreamReader(pokeNameFilePath, Encoding.UTF8);
            var input = await inputStream.ReadToEndAsync();
            inputStream.Close();

            // ���зָ�
            string[] inputString = input.Split(['\r', '\n']);

            List<string> sourceList = [];

            // ȥ�����к��Ʊ��
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

            // ����pokeNameList
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
                    MessageBox.Show("�������ʧ�ܣ����������ļ�", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // ȥ�����������ķ�����ע
                for (int i = 0; i < list.Count; i++)
                {
                    string[] _temp = list[i].Split(" (");
                    if (_temp.Length > 1)
                    {
                        list[i] = _temp[0];
                    }

                    // ת���ַ�
                    list[i] = SQLiteEscapingSpecialCharacters(list[i]);
                }

                StringBuilder sb = new();
                foreach (var line in list)
                {
                    sb.Append(line);
                    sb.Append(',');
                }

                // ɾ�����һ������
                if (sb.Length > 0)
                {
                    sb.Length--;
                }

                string output = sb.ToString();
                pokeNameList.Add(output);
            }
        }

        // ��ȡMega����
        // ����ԴӦ������ȫ��ͼ�����
        // ��Ҫ����ʵ������Դ
        private async Task ReadMegaPokeNameFromFile()
        {
            pokeMegaNameList.Clear();

            // ��ȡԴ�ļ�
            var inputStream = new StreamReader(pokeMegaNameFilePath, Encoding.UTF8);
            var input = await inputStream.ReadToEndAsync();
            inputStream.Close();

            // ���зָ�
            string[] inputString = input.Split(['\r', '\n']);

            List<string> sourceList = [];

            // ȥ�����к��Ʊ��
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

            // ����pokeMegaNameList
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
                    MessageBox.Show("�������ʧ�ܣ����������ļ�", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // ȥ�����������ķ�����ע
                for (int i = 0; i < list.Count; i++)
                {
                    string[] _temp = list[i].Split(" (");
                    if (_temp.Length > 1)
                    {
                        list[i] = _temp[0];
                    }

                    // ת���ַ�
                    list[i] = SQLiteEscapingSpecialCharacters(list[i]);
                }

                StringBuilder sb = new();
                foreach (var line in list)
                {
                    sb.Append(line);
                    sb.Append(',');
                }

                // ɾ�����һ������
                if (sb.Length > 0)
                {
                    sb.Length--;
                }

                string output = sb.ToString();
                pokeMegaNameList.Add(output);
            }
        }

        // ��ȡ��ȡ����ֵ������
        private async Task ReadPokeDataFromFile()
        {
            pokeDatas.Clear();
            megaPokeDatas.Clear();

            // ��ȡԴ�ļ�
            var inputStream = new StreamReader(pokeDataFilePath, Encoding.UTF8);
            var input = await inputStream.ReadToEndAsync();
            inputStream.Close();

            // ���зָ�
            string[] separator = ["--------------------------------------------"];
            string[] inputString = input.Split(separator, StringSplitOptions.None);

            List<List<string>> pokeDataLists = [];

            // ȥ�����к��Ʊ��
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

            // �ֿ�����
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

            // ɸѡmega
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

            // ������ͨ����
            List<string> pokeNameList = [];

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

                // ��ȡ����
                string[] temp_type = item[7].Split(':')[1].Split('/');
                foreach (var line in temp_type)
                {
                    if (line != "")
                    {
                        poke.type.Add(line.Trim());
                    }
                }

                // ��ȡ����
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

                // ��ȡBST
                string[] temp_BST = item[5].Split(':')[1].Trim().Split('.');
                for (int i = 0; i < temp_BST.Length; i++)
                {
                    try
                    {
                        poke.BST[i] = Int16.Parse(temp_BST[i]);
                    }
                    catch
                    {
                        MessageBox.Show("BST����һ����Ч��ֵ", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                // ��ȡ�����׶�
                string stage = item[2].Split(':')[1].Trim();
                try
                {
                    poke.evolutionaryStage = Int16.Parse(stage);
                }
                catch
                {
                    MessageBox.Show("�����׶β���һ����Ч��ֵ", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // ��ѯȫ��ͼ�����
                GetNationalNumberAndNameDict(DBPokeTable, nationalNumberAndNamesDict);

                foreach (var kvp in nationalNumberAndNamesDict)
                {
                    if (pokeName == kvp.Value[7])
                    {
                        poke.nationalNumber = kvp.Key;
                    }
                }

                // �ж��Ƿ����ս׶�
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

                // �ж��Ƿ�˵
                if (PokeData.pokeLegendaryList.Contains(poke.nationalNumber))
                {
                    poke.ifLegendary = true;
                }
                else
                {
                    poke.ifLegendary = false;
                }

                // �������б�
                pokeDatas.Add(poke);
            }

            // ����mega����
            List<string> pokeNameMegaList = [];

            foreach (var item in megaPokes)
            {
                var pokeName = "����" + item[1].Trim().Split(' ')[0];
                if (!pokeNameMegaList.Contains(pokeName))
                {
                    pokeNameMegaList.Add(pokeName);
                }
                else
                {
                    continue;
                }

                var poke = new PokeData(pokeName);

                // ��ȡ����
                string[] temp_type = item[7].Split(':')[1].Split('/');
                foreach (var line in temp_type)
                {
                    if (line != "")
                    {
                        poke.type.Add(line.Trim());
                    }
                }

                // ��ȡ����
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

                // ��ȡBST
                string[] temp_BST = item[5].Split(':')[1].Trim().Split('.');
                for (int i = 0; i < temp_BST.Length; i++)
                {
                    try
                    {
                        poke.BST[i] = Int16.Parse(temp_BST[i]);
                    }
                    catch
                    {
                        MessageBox.Show("BST����һ����Ч��ֵ", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                // ��ȡ�����׶�
                string stage = item[2].Split(':')[1].Trim();
                try
                {
                    poke.evolutionaryStage = Int16.Parse(stage);
                }
                catch
                {
                    MessageBox.Show("�����׶β���һ����Ч��ֵ", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // ��ѯȫ��ͼ�����
                GetNationalNumberAndNameDict(DBMegaTable, nationalNumberAndMegaNamesDict);

                foreach (var kvp in nationalNumberAndMegaNamesDict)
                {
                    if (pokeName == kvp.Value[7])
                    {
                        poke.nationalNumber = kvp.Key;
                    }
                }

                // �ж��Ƿ����ս׶�
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

                // �ж��Ƿ�˵
                if (PokeData.pokeLegendaryList.Contains(poke.nationalNumber))
                {
                    poke.ifLegendary = true;
                }
                else
                {
                    poke.ifLegendary = false;
                }

                // �������б�
                megaPokeDatas.Add(poke);
            }
        }

        // ���߼�
        private async void BTN_Gen_Click(object sender, EventArgs e)
        {
            if (pokeNameFilePath == string.Empty || pokeDataFilePath == string.Empty)
            {
                return;
            }

            BTN_Gen.Enabled = false;

            startTime = DateTime.Now;
            timer.Start();

            // ɾ�����е����ݿ�
            try
            {
                DeleteDB();
            }
            catch
            {
                timer.Stop();
                CleanFormTitle();
                BTN_Gen.Enabled = true;
                MessageBox.Show("ɾ���������ݿ�ʧ��,\n��ȷ��û�б���������ռ�á�", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // ������������
            await ReadPokeDataFromFile();

            // �������ݿ�
            UpdatePokeDatas(DBPokeTable, pokeDatas);
            UpdatePokeDatas(DBMegaTable, megaPokeDatas);

            BTN_Gen.Enabled = true;

            timer.Stop();
            CleanFormTitle();

            // ��ʾ���
            MessageBox.Show("�������\n��ʱ��" + elapsedTime.ToString(@"mm\��ss\��"), "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        // ����������
        private const string PokeWikiRootLink = "https://wiki.52poke.com";
        private const string PokeSumLink = "https://wiki.52poke.com/wiki/%E5%AE%9D%E5%8F%AF%E6%A2%A6%E5%88%97%E8%A1%A8%EF%BC%88%E6%8C%89%E5%85%A8%E5%9B%BD%E5%9B%BE%E9%89%B4%E7%BC%96%E5%8F%B7%EF%BC%89/%E7%AE%80%E5%8D%95%E7%89%88";
        private static List<string> pokeLinksList = [];
        private static List<PokeData> pokeStats = [];

        private async Task GetPokeLinks()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    // �����û�����
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                    var html = await httpClient.GetStringAsync(PokeSumLink);
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(html);

                    // ѡ�����е�trԪ��
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

                                Debug.WriteLine($"ȫ��ͼ����: {firstCell}, ����: {secondCell}, ����: {link}");
                                TB_Info.AppendText($"ȫ��ͼ����: {firstCell}, ����: {secondCell}, ����: {link}\r\n");

                                // debug
                                if (_count >= 20)
                                {
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("��ǰ�׶Σ�[��ȡȫ������]��û���ҵ��κ�trԪ�ء�");
                        TB_Info.AppendText("��ǰ�׶Σ�[��ȡȫ������]��û���ҵ��κ�trԪ�ء�\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                TB_Info.AppendText($"��ǰ�׶Σ�[��ȡȫ������]����������: {ex.Message}\r\n");
                MessageBox.Show($"��ǰ�׶Σ�[��ȡȫ������]����������: {ex.Message}");
            }
        }

        private static string RemoveParentheses(string input)
        {
            string pattern = @"[����()]";
            return Regex.Replace(input, pattern, string.Empty);
        }

        private static string RemoveParenthesesAndInside(string input)
        {
            string pattern = @"[��(].*[��)]";
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
            Debug.WriteLine($"ԭʼ��{input}\n����{output}");
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
                    } else
                    {
                        return string.Empty;
                    }
                }
            } else
            {
                return description;
            }
        }

        private async Task GetPokeBasicStats()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                    int _count = 0;
                    foreach (var pokeLink in pokeLinksList)
                    {
                        _count++;

                        // ����ӳ�
                        bool shouldDelay = false;
                        byte[] buffer = Guid.NewGuid().ToByteArray();
                        int iSeed = BitConverter.ToInt32(buffer, 0);
                        Random random = new Random(iSeed);
                        int delay = random.Next(50, 1050);
                        if (shouldDelay)
                        {
                            Debug.WriteLine($"��ǰ�׶Σ�[��ȡ������Ϣ]���������ߣ�{delay} ����");
                            Thread.Sleep(delay);
                        }

                        var html = await httpClient.GetStringAsync(pokeLink);
                        var htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(html);

                        // ��ȡ���б�Ҫ��Ϣ��Ȼ��洢��pokedata����

                        // ��ȡ�����ڵ�
                        var basicStatsTable = htmlDoc.DocumentNode.SelectNodes("//table[contains(@class, 'roundy')]")[2];

                        if (basicStatsTable != null)
                        {
                            // ��ȡ������Ϣ
                            var trRows = basicStatsTable.Element("tbody").Elements("tr").ToList();

                            // ����
                            var row1 = trRows[0];
                            var name = row1.Descendants("b").First().InnerText;

                            // ȫ��ͼ����
                            var _pokedexNumber = row1.Descendants("th").First().InnerText.Replace('#', '0');
                            int.TryParse(_pokedexNumber, out int pokedexNumber);

                            // ����
                            List<string> type = [];
                            var row3 = trRows[2];
                            var types = row3.Descendants("a").ToList();
                            type.Add(types[1].InnerText);
                            if (!types[2].InnerText.Contains("����"))
                            {
                                type.Add(types[2].InnerText);
                            }

                            // ����
                            var row3td2 = row3.Elements("td").Last();
                            var category = row3td2.Descendants("a").ToList().Last().InnerText;

                            // ����
                            List<string> abilities = [];
                            List<string> hiddenAbilities = [];
                            bool hiddenAbilitiesExist = true;
                            var row4 = trRows[3];
                            var abilityNodes = row4.Descendants("tr").Last().Elements("td").ToList();

                            if (abilityNodes.Count > 1)
                            {
                                // ��ͨ����
                                foreach (var _ability in abilityNodes[0].Elements("a"))
                                {
                                    abilities.Add(_ability.InnerText);
                                }

                                // ��������
                                foreach (var _ability in abilityNodes[1].Elements("a"))
                                {
                                    hiddenAbilities.Add(_ability.InnerText);
                                }
                            } else
                            {
                                hiddenAbilitiesExist = false;

                                var _ability = row4.Element("td").Descendants("a").Last().InnerText;
                                abilities.Add(_ability);
                            }

                            // ��������
                            string levelingRate, height, weight, shape, pokedexColor;
                            int catchRate, hatchTime;
                            List<float> genderRatio = [];
                            List<string> eggGroups = [];
                            List<int> EVYield = [];

                            if (hiddenAbilitiesExist)
                            {
                                // ���������ٶ�
                                var row5 = trRows[4];
                                var _levelingRate = row5.Descendants("small").ToList().First().InnerText;
                                levelingRate = RemoveParentheses(_levelingRate);

                                // ��ߡ�����
                                var row8 = trRows[7];
                                height = row8.Elements("td").ToList()[0].Descendants("td").ToList()[1].InnerText.Trim();
                                weight = row8.Elements("td").ToList()[1].Descendants("td").ToList()[1].InnerText.Trim();

                                // ����
                                var row9 = trRows[8];
                                shape = row9.Descendants("img").First().GetAttributeValue("alt", "");

                                // ͼ����ɫ��������
                                var row10 = trRows[9];
                                pokedexColor = row10.Elements("td").ToList()[0].Descendants("span").First().InnerText;
                                var _catchRate = row10.Elements("td").ToList()[1].Descendants("td").Last().InnerText;
                                _catchRate = RemoveParenthesesAndInside(_catchRate);
                                int.TryParse(_catchRate, out catchRate);

                                // �Ա����
                                var row11 = trRows[10];
                                var _genderRatio = row11.Descendants("span").First().InnerText;
                                float.TryParse(ExtractNumber(_genderRatio), out var maleRatio);
                                var femaleRatio = 100 - maleRatio;
                                genderRatio.Add(maleRatio);
                                genderRatio.Add(femaleRatio);

                                // ��Ⱥ����������
                                var row12 = trRows[11];
                                var row12tr2 = row12.Descendants("tr").Last().Descendants("td").ToList();
                                var _eggGroupsNode = row12tr2[0].Descendants("a").ToList();
                                foreach (var node in _eggGroupsNode)
                                {
                                    eggGroups.Add(node.InnerText);
                                }
                                var _hatchTime = row12tr2[1].InnerText;
                                int.TryParse(ExtractHatchTime(_hatchTime), out hatchTime);

                                // ȡ�û�������
                                var row13 = trRows[12];
                                var row13tr2 = row13.Descendants("tr").ToList()[1];
                                var _EVYield = row13tr2.Elements("td").ToList();
                                foreach(var node in _EVYield)
                                {
                                    var _ev = node.InnerText.Trim();
                                    int.TryParse(ExtractTailNumber(_ev), out var ev);
                                    EVYield.Add(ev);
                                }
                            }
                            else
                            {
                                // ���������ٶ�
                                var _levelingRate = row4.Elements("td").ToList().Last().Descendants("small").ToList().First().InnerText;
                                levelingRate = RemoveParentheses(_levelingRate);

                                // ��ߡ�����
                                var row7 = trRows[6];
                                height = row7.Elements("td").ToList()[0].Descendants("td").ToList()[1].InnerText.Trim();
                                weight = row7.Elements("td").ToList()[1].Descendants("td").ToList()[1].InnerText.Trim();

                                // ����
                                var row8 = trRows[7];
                                shape = row8.Descendants("img").First().GetAttributeValue("alt", "");

                                // ͼ����ɫ��������
                                var row9 = trRows[8];
                                pokedexColor = row9.Elements("td").ToList()[0].Descendants("span").First().InnerText;
                                var _catchRate = row9.Elements("td").ToList()[1].Descendants("td").Last().InnerText;
                                _catchRate = RemoveParenthesesAndInside(_catchRate);
                                int.TryParse(_catchRate, out catchRate);

                                // �Ա����
                                var row10 = trRows[9];
                                var _genderRatio = row10.Descendants("span").First().InnerText;
                                float.TryParse(ExtractNumber(_genderRatio), out var maleRatio);
                                var femaleRatio = 100 - maleRatio;
                                genderRatio.Add(maleRatio);
                                genderRatio.Add(femaleRatio);

                                // ��Ⱥ����������
                                var row11 = trRows[10];
                                var row11tr2 = row11.Descendants("tr").Last().Descendants("td").ToList();
                                var _eggGroupsNode = row11tr2[0].Descendants("a").ToList();
                                foreach (var node in _eggGroupsNode)
                                {
                                    eggGroups.Add(node.InnerText);
                                }
                                var _hatchTime = row11tr2[1].InnerText;
                                int.TryParse(ExtractHatchTime(_hatchTime), out hatchTime);

                                // ȡ�û�������
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
                            List<string> learnsetLevelingUp = [];

                            // ����ֵ
                            var rowHP = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'bgl-HP')]").First();
                            var _HP = rowHP.Element("th").Descendants("span").Last().InnerText;
                            int.TryParse(_HP, out var HP);
                            baseStats.Add(HP);

                            var rowATK = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'bgl-����')]").First();
                            var _ATK = rowATK.Element("th").Descendants("span").Last().InnerText;
                            int.TryParse(_ATK, out var ATK);
                            baseStats.Add(ATK);

                            var rowDEF = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'bgl-����')]").First();
                            var _DEF = rowDEF.Element("th").Descendants("span").Last().InnerText;
                            int.TryParse(_DEF, out var DEF);
                            baseStats.Add(DEF);

                            var rowSPA = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'bgl-�ع�')]").First();
                            var _SPA = rowSPA.Element("th").Descendants("span").Last().InnerText;
                            int.TryParse(_SPA, out var SPA);
                            baseStats.Add(SPA);

                            var rowSPD = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'bgl-�ط�')]").First();
                            var _SPD = rowSPD.Element("th").Descendants("span").Last().InnerText;
                            int.TryParse(_SPD, out var SPD);
                            baseStats.Add(SPD);

                            var rowSPE = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'bgl-�ٶ�')]").First();
                            var _SPE = rowSPE.Element("th").Descendants("span").Last().InnerText;
                            int.TryParse(_SPE, out var SPE);
                            baseStats.Add(SPE);

                            // ͼ������
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
                            } else
                            {
                                Debug.WriteLine("descriptionTableΪ��");
                                pokedexDescription = string.Empty;
                            }

                            // ��ѧ�����ʽ
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
                                    _level = "0";
                                }
                                int.TryParse(_level, out int level);
                                var move = td[2].Descendants("a").First().InnerText.Trim();
                                var output = level + "-" + move;
                                // ȥ��
                                bool exist = false;
                                foreach (var item in learnsetLevelingUp)
                                {
                                    if (item == output)
                                    {
                                        exist = true;
                                        break;
                                    }
                                }
                                if (!exist)
                                {
                                    learnsetLevelingUp.Add(output);
                                }
                            }

                            // ��ʹ�õ���ʽѧϰ��

                            // debug
                            string typeOutput = string.Join(", ", type);
                            string abilitiesOutput = string.Join(", ", abilities);
                            string hiddenAbilitiesOutput = string.Join(", ", hiddenAbilities);
                            string genderRatioOutput = string.Join(", ", genderRatio);
                            string eggGroupsOutput = string.Join(", ", eggGroups);
                            string EVYieldOutput = string.Join(", ", EVYield);
                            string baseStatsOutput = string.Join(", ", baseStats);
                            string learnsetLevelingUpOutput = string.Join(", ", learnsetLevelingUp);

                            var outputInfo = $"{pokedexNumber}-{name}-{typeOutput}-{category}-��ͨ����:{abilitiesOutput}-��������:{hiddenAbilitiesOutput}-���������ٶ�:{levelingRate}" +
                                $"-���:{height}-����{weight}-����:{shape}-ͼ����ɫ:{pokedexColor}-������:{catchRate}-�Ա����:{genderRatioOutput}-��Ⱥ:{eggGroupsOutput}" +
                                $"-��������:{hatchTime}-ȡ�û�������:{EVYieldOutput}-����ֵ:{baseStatsOutput}-ͼ������:{pokedexDescription}-��ѧ�����ʽ:{learnsetLevelingUpOutput}";
                            Debug.WriteLine(outputInfo);
                            TB_Info.AppendText(outputInfo + "\r\n");
                            Debug.WriteLine($"��ǰ�׶Σ�[��ȡ������Ϣ]���Ѵ�����Ŀ��{_count} / {pokeLinksList.Count}");
                            TB_Info.AppendText($"��ǰ�׶Σ�[��ȡ������Ϣ]���Ѵ�����Ŀ��{_count} / {pokeLinksList.Count}\r\n");
                        }
                        else
                        {
                            Debug.WriteLine($"��ǰ�׶Σ�[��ȡ������Ϣ]��û���ҵ��κ�ƥ��Ԫ�أ���ǰ��Ŀ��{_count}");
                            TB_Info.AppendText($"��ǰ�׶Σ�[��ȡ������Ϣ]��û���ҵ��κ�ƥ��Ԫ�أ���ǰ��Ŀ��{_count}\r\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TB_Info.AppendText($"��ǰ�׶Σ�[��ȡ������Ϣ]����������: {ex.Message}\r\n");
                MessageBox.Show($"��ǰ�׶Σ�[��ȡ������Ϣ]����������: {ex.Message}");
            }
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            TB_Info.Clear();
            await GetPokeLinks();
            await GetPokeBasicStats();

            //TestPattern();
        }
    }
}
