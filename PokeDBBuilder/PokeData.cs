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
    }
}
