using System.Data.SQLite;
using System.Text;

namespace PokeDBBuilder
{
    public partial class Form1 : Form
    {
        private readonly static string pokeDB = "PokeDB.sqlite";
        private readonly static string DBTableName = "pokedata";
        private static string pokeNameFilePath = string.Empty;
        private static string pokeDataFilePath = string.Empty;
        private static string pokeDBPath = string.Empty;

        private static List<string> pokeNameList = [];

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

        private void ExecuteSQLiteCommand(string commandIn)
        {
            string dbFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), pokeDB);
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbFilePath};Version=3;"))
            {
                connection.Open();
                SQLiteCommand command = new SQLiteCommand(commandIn, connection);
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

            // ������
            var createTable = new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS " + DBTableName + " (" +
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

            connection.Close();
        }

        private void InsertPokeName()
        {
            var connection = CreateSQLiteConnection();
            connection.Open();

            // ��������
            foreach (var pokeName in pokeNameList) {
                var insertData = new SQLiteCommand("INSERT INTO " + DBTableName + " (name) VALUES ('" + pokeName + "')", connection);
                try {
                    insertData.ExecuteNonQuery();
                } catch {
                    MessageBox.Show(pokeName, "Debug", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            connection.Close();
        }

        // ��bug
        private void PromoteData()
        {
            var connection = CreateSQLiteConnection();
            connection.Open();

            var promote = new SQLiteCommand("SELECT * FROM " + DBTableName, connection);
            using (SQLiteDataReader reader = promote.ExecuteReader())
            {
                while (reader.Read())
                {
                    StringBuilder sb = new();
                    foreach (var item in reader) {
                        sb.Append(item.ToString());
                        sb.Append("|||");
                    }
                    string temp = sb.ToString();
                    // MessageBox.Show(temp, "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.None);
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
            foreach (var source in sourceList) {
                string[] temp = source.Split(',');
                List<string> list = [];

                try
                {
                    for (int i = 2; i < 19; i += 2)
                    {
                        list.Add(temp[i]);
                    }
                } catch {
                    MessageBox.Show("�������ʧ�ܣ����������ļ�", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // ȥ�����������ķ�����ע
                for (int i = 0; i < list.Count; i ++)
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
                foreach (var line in list) {
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

        // ���߼�
        private async void BTN_Gen_Click(object sender, EventArgs e)
        {
            //MessageBox.Show(pokeNameFilePath + "\n" + pokeDataFilePath, "Debug", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            if (pokeNameFilePath == string.Empty)
            {
                return;
            }

            CreateTable();

            await ReadPokeNameFromFile();

            InsertPokeName();

            PromoteData();

            // ��ʾ���
            MessageBox.Show("�������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }
}
