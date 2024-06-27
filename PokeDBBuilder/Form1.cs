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
                // ���Ƶ�Mega��
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

            // ��ѯname�е�ֵ���������ŷָ�
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
                } else if (i == 8)
                {
                    elements[i] = "����" + elements[i];
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

            List<string> pokeDataNameList = [];
            List<string> pokeDataBlackList = ["�ҹ��� 1", "������� 1", "�и����� 1", "����� 1", "�ο������� 3", "�ο������� 2", "�ο������� 1", "��β�ۼ��� 1", "����� 1", "��󫻨 1", "��ũ�ڳ� 1", "è��̽�� 1",
                "Ƥ���� 7", "Ƥ���� 6", "Ƥ���� 5", "Ƥ���� 4", "Ƥ���� 3", "Ƥ���� 2", "Ƥ���� 1", "������� 1", "������ 3", "������ 2", "������ 1", "������ 1", "���� 1", "С���� 13", "С���� 12", "С���� 11",
                "С���� 10", "С���� 9", "С���� 8", "С���� 7", "С���� 6", "С���� 5", "С���� 4", "С���� 3", "С���� 2", "С���� 1", "������� 4", "������� 3", "������� 2", "������� 1", "�׺����� 2",
                "�׺����� 1", "�������� 2", "�������� 1", "Ҭ���� 1", "������ 1", "���� 1", "¡¡�� 1", "¡¡ʯ 1", "Сȭʯ 1", "è�ϴ� 1", "���� 1", "��β 1", "��β 1", "��ɽ�� 1", "��ɽ�� 1", "���� 1", "���� 2",
                "���� 1", "С���� 1", "�������� 2", "�������� 1", "������ 3", "������ 2", "������ 1", "������ 1", "��Ҷ�� 5", "��Ҷ�� 4", "��Ҷ�� 3", "��Ҷ�� 2", "��Ҷ�� 1", "�ϹϹ��� 3", "�ϹϹ��� 2", "�ϹϹ��� 1",
                "�ϹϾ� 3", "�ϹϾ� 2", "�ϹϾ� 1", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""];

            // ��������
            foreach (var item in pokeDataLists)
            {
                var pokeName = item[1].Trim();

                // ����Mega����
                bool ifMegaForm = pokeName.Contains('1') && !pokeDataBlackList.Contains(pokeName);

                if (ifMegaForm)
                {
                    pokeName = pokeName.Replace(" 1", "");
                    pokeName = "����" + pokeName;
                }

                if (!pokeDataNameList.Contains(pokeName) && !pokeDataBlackList.Contains(pokeName))
                {
                    pokeDataNameList.Add(pokeName);
                } else
                {
                    continue;
                }

                var poke = new PokeData(pokeName);

                // �ж��Ƿ�Mega
                if (ifMegaForm)
                {
                    poke.ifMegaForm = ifMegaForm;
                }

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

                // �ж��Ƿ����ս׶�
                if (PokeData.pokeFinalStageList.Contains(poke.nationalNumber))
                {
                    poke.ifFinalStage = true;
                } else
                {
                    poke.ifFinalStage = false;
                }

                // �ж��Ƿ�˵
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

        // ���߼�
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

            // ������������
            await ReadPokeDataFromFile();

            // �������ݿ�

            BTN_Gen.Enabled = true;

            // ��ʾ���
            MessageBox.Show("�������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }
}
