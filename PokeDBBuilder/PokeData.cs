namespace PokeDBBuilder
{
    internal class PokeData
    {
        public int nationalNumber;
        public string name;
        public List<string> type = [];
        public List<string> abilities = [];
        public int[] BST = new int[6];
        public int evolutionaryStage;
        public bool ifFinalStage;
        public bool ifMegaForm;
        public bool ifLegendary;

        // 特殊列表
        public static readonly List<int> pokeMegaListFromXY = [3, 6, 9, 65, 94, 115, 127, 130, 142, 150, 181, 212, 214, 229, 248, 257, 282, 303, 306, 308, 310, 354, 359, 380, 381, 445, 448, 460];
        public static readonly List<int> pokeMegaListFromORAS = [15, 18, 80, 208, 254, 260, 302, 319, 323, 334, 362, 373, 376, 384, 428, 475, 531, 719];
        public static List<int> pokeMegaList = [];
        public static readonly List<int> pokeLegendaryList = [144, 145, 146, 243, 244, 245, 377, 378, 379, 380, 381, 480, 481, 482, 485, 486, 488, 638, 639, 640, 641, 642, 645, 772, 773, 785, 786,
            787, 788, 891, 892, 894, 895, 896, 897, 144, 145, 146, 905, 1001, 1002, 1003, 1004, 1014, 1015, 1016, 1017, 150, 249, 250, 382, 383, 384, 483, 484, 487, 643, 644, 646, 716, 717, 718,
            789, 790, 791, 792, 800, 888, 889, 890, 898, 1007, 1008, 1024];
        public static readonly List<int> pokeFinalStageList = [3, 6, 9, 12, 15, 18, 20, 22, 24, 25, 26, 28, 31, 34, 36, 38, 40, 45, 47, 49, 51, 53, 55, 59, 62, 65, 68, 71, 73, 76, 78, 80, 83, 85,
            87, 89, 91, 94, 97, 99, 101, 103, 105, 106, 107, 110, 115, 119, 121, 122, 124, 127, 128, 130, 131, 132, 133, 134, 135, 136, 139, 141, 142, 143, 144, 145, 146, 149, 150, 151, 154, 157,
            160, 162, 164, 166, 168, 169, 171, 178, 181, 182, 184, 185, 186, 189, 192, 195, 196, 197, 199, 201, 202, 205, 208, 210, 211, 212, 213, 214, 219, 222, 224, 225, 226, 227, 229, 230, 232,
            235, 237, 241, 242, 243, 244, 245, 248, 249, 250, 251, 254, 257, 260, 262, 264, 267, 269, 272, 275, 277, 279, 282, 284, 286, 289, 291, 292, 295, 297, 301, 302, 303, 306, 308, 310, 311,
            312, 313, 314, 317, 319, 321, 323, 324, 326, 327, 330, 332, 334, 335, 336, 337, 338, 340, 342, 344, 346, 348, 350, 351, 352, 354, 357, 358, 359, 362, 365, 367, 368, 369, 370, 373, 376,
            377, 378, 379, 380, 381, 382, 383, 384, 385, 386, 389, 392, 395, 398, 400, 402, 405, 407, 409, 411, 413, 414, 416, 417, 419, 421, 423, 424, 426, 428, 429, 430, 432, 435, 437, 441, 442,
            445, 448, 450, 452, 454, 455, 457, 460, 461, 462, 463, 464, 465, 466, 467, 468, 469, 470, 471, 472, 473, 474, 475, 476, 477, 478, 479, 480, 481, 482, 483, 484, 485, 486, 487, 488, 489,
            490, 491, 492, 493, 494, 497, 500, 503, 505, 508, 510, 512, 514, 516, 518, 521, 523, 526, 528, 530, 531, 534, 537, 538, 539, 542, 545, 547, 549, 553, 555, 556, 558, 560, 561, 563, 565,
            567, 569, 571, 573, 576, 579, 581, 584, 586, 587, 589, 591, 593, 594, 596, 598, 601, 604, 606, 609, 612, 614, 615, 617, 618, 620, 621, 623, 626, 628, 630, 631, 632, 635, 637, 638, 639,
            640, 641, 642, 643, 644, 645, 646, 647, 648, 649, 652, 655, 658, 660, 663, 666, 668, 671, 673, 675, 676, 678, 681, 683, 685, 687, 689, 691, 693, 695, 697, 699, 700, 701, 702, 703, 706,
            707, 709, 711, 713, 715, 716, 717, 718, 719, 720, 721, 724, 727, 730, 733, 735, 738, 740, 741, 743, 745, 746, 748, 750, 752, 754, 756, 758, 760, 763, 764, 765, 766, 768, 770, 771, 773,
            774, 775, 776, 777, 778, 779, 780, 781, 784, 785, 786, 787, 788, 791, 792, 793, 794, 795, 796, 797, 798, 799, 800, 801, 802, 804, 805, 806, 807, 809, 812, 815, 818, 820, 823, 826, 828,
            830, 832, 834, 836, 839, 841, 842, 844, 845, 847, 849, 851, 853, 855, 858, 861, 862, 863, 864, 865, 866, 867, 869, 870, 871, 873, 874, 875, 876, 877, 879, 880, 881, 882, 883, 887, 888,
            889, 890, 892, 893, 894, 895, 896, 897, 898, 899, 900, 901, 902, 903, 904, 905, 908, 911, 914, 916, 918, 920, 923, 925, 927, 930, 931, 934, 936, 937, 939, 941, 943, 945, 947, 949, 950,
            952, 954, 956, 959, 961, 962, 964, 966, 967, 968, 970, 972, 973, 975, 976, 977, 978, 979, 980, 981, 982, 983, 984, 985, 986, 987, 988, 989, 990, 991, 992, 993, 994, 995, 998, 1000, 1001,
            1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010, 1013, 1014, 1015, 1016, 1017, 1018, 1019, 1020, 1021, 1022, 1023, 1024, 1025];

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

        public static List<int> getLegendaryList()
        {
            QuickSort(pokeLegendaryList, 0, pokeLegendaryList.Count - 1);
            return pokeLegendaryList;
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
