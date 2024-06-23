using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq.Expressions;
using System.Text;

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
        private static readonly List<int> pokeMegaDexListFromXY = [3, 6, 9, 65, 94, 115, 127, 130, 142, 150, 181, 212, 214, 229, 248, 257, 282, 303, 306, 308, 310, 354, 359, 380, 381, 445, 448, 460];
        private static readonly List<int> pokeMegaDexListFroORAS = [15, 18, 80, 208, 254, 260, 302, 319, 323, 334, 362, 373, 376, 384, 428, 475, 531, 719];
        private static List<int> pokeMegaDexList = [];

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

        private void InserPokeDexNumber()
        {
            var connection = CreateSQLiteConnection();
            connection.Open();

            var updatePokeDexNumber = new SQLiteCommand("UPDATE " + DBPokeTable + " SET nationalNumber = ID", connection);
            updatePokeDexNumber.ExecuteNonQuery();

            if (pokeMegaNameFilePath == string.Empty)
            {
                // ���Ƶ�Mega��
                StringBuilder sb = new();
                foreach (var item in pokeMegaDexList)
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
            return string.Join(", ", elements);
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

            if (openFileDialog.FileName == "ѡ��һ��txt�ļ�")
            {
                MessageBox.Show("��ѡ��һ���ļ���", "δѡ���ļ�", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
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

            if (openFileDialog.FileName == "ѡ��һ��txt�ļ�")
            {
                MessageBox.Show("��ѡ��һ���ļ���", "δѡ���ļ�", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
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

            if (openFileDialog.FileName == "ѡ��һ��txt�ļ�")
            {
                MessageBox.Show("��ѡ��һ���ļ���", "δѡ���ļ�", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
        }

        // ��ȡ����
        private async static Task ReadPokeNameFromFile()
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
        private async static Task ReadMegaPokeNameFromFile()
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

        // ��������
        static void QuickSort(List<int> list, int low, int high)
        {
            if (low < high)
            {
                int pi = Partition(list, low, high);

                QuickSort(list, low, pi - 1);
                QuickSort(list, pi + 1, high);
            }
        }

        static int Partition(List<int> list, int low, int high)
        {
            int pivot = list[high];
            int i = low - 1;

            for (int j = low; j <= high - 1; j++)
            {
                if (list[j] < pivot)
                {
                    i++;
                    int _temp = list[i];
                    list[i] = list[j];
                    list[j] = _temp;
                }
            }
            int temp = list[i + 1];
            list[i + 1] = list[high];
            list[high] = temp;
            return (i + 1);
        }

        // �Զ�����Mega Name����
        private static void GenMegaNameFromDB()
        {
            pokeMegaDexList = pokeMegaDexListFromXY.Concat(pokeMegaDexListFroORAS).ToList();
            QuickSort(pokeMegaDexList, 0, pokeMegaDexList.Count - 1);
        }

        // ���߼�
        private async void BTN_Gen_Click(object sender, EventArgs e)
        {
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
            else
            {
                GenMegaNameFromDB();
            }

            InserPokeDexNumber();

            UpdateMegaName();

            // ��ʾ���
            MessageBox.Show("�������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }
}
