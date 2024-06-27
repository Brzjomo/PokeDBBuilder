namespace PokeDBBuilder
{
    internal class PokeData
    {
        public int nationalNumber;
        public string name;
        public List<string>? type;
        public List<string>? abilities;
        public int[] BST = new int[6];
        public int evolutionaryStage;
        public bool ifFinalStage;
        public bool ifMegaForm;
        public bool ifLegendary;

        // 特殊列表
        public static readonly List<int> pokeMegaListFromXY = [3, 6, 9, 65, 94, 115, 127, 130, 142, 150, 181, 212, 214, 229, 248, 257, 282, 303, 306, 308, 310, 354, 359, 380, 381, 445, 448, 460];
        public static readonly List<int> pokeMegaListFromORAS = [15, 18, 80, 208, 254, 260, 302, 319, 323, 334, 362, 373, 376, 384, 428, 475, 531, 719];
        public static List<int> pokeMegaList = [];
        public static readonly List<int> pokeLegendaryList = [0, 1, 2];
        public static readonly List<int> pokeFinalStageList = [0, 1, 2];

        public PokeData(string _name) {
            name = _name;
        }

        public PokeData(int _nationalNumber, string _name, List<string> _type, List<string> _abilities,
            int[] _BST, int _evolutionaryStage, bool _ifFinalStage, bool _ifMegaForm, bool _ifLegendary)
        {
            nationalNumber = _nationalNumber;
            name = _name;
            type = _type;
            abilities = _abilities;
            BST = _BST;
            evolutionaryStage = _evolutionaryStage;
            ifFinalStage = _ifFinalStage;
            ifMegaForm = _ifMegaForm;
            ifLegendary = _ifLegendary;
        }

        public static List<int> getMegaList()
        {
            pokeMegaList = [..pokeMegaListFromXY, ..pokeMegaListFromORAS];
            QuickSort(pokeMegaList, 0, pokeMegaList.Count - 1);
            return pokeMegaList;
        }

        // 快速排序
        public static void QuickSort(List<int> list, int low, int high)
        {
            if (low < high)
            {
                int pi = Partition(list, low, high);

                QuickSort(list, low, pi - 1);
                QuickSort(list, pi + 1, high);
            }
        }

        private static int Partition(List<int> list, int low, int high)
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
    }
}
