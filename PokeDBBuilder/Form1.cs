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

            // 创建表
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

            // 插入数据
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

        // 有bug
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
                    // MessageBox.Show(temp, "提示", MessageBoxButtons.OK, MessageBoxIcon.None);
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

            if (openFileDialog.FileName == "选择一个txt文件")
            {
                MessageBox.Show("请选择一个文件。", "未选择文件", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
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

            if (openFileDialog.FileName == "选择一个txt文件")
            {
                MessageBox.Show("请选择一个文件。", "未选择文件", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
        }

        // 读取名称
        private async static Task ReadPokeNameFromFile()
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
                    MessageBox.Show("名称添加失败，请检查数据文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 去掉日语跟韩语的发音标注
                for (int i = 0; i < list.Count; i ++)
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
                foreach (var line in list) {
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

        // 主逻辑
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

            // 提示完成
            MessageBox.Show("运行完毕", "提示", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }
}
