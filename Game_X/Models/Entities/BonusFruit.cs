namespace Game_X.Models.Entities
{
    public class BonusFruit
    {
        public int X { get; set; } = 13;
        public int Y { get; set; } = 17;
        public bool Active { get; set; } = false;
        public int Type { get; set; } = 0; // 0: Cereja, 1: Morango, 2: Laranja, 3: Maçã, 4: Melancia

        // Pontuação baseada no tipo de fruta
        public int Points => Type switch
        {
            0 => 100,  // Cereja
            1 => 300,  // Morango
            2 => 500,  // Laranja
            3 => 700,  // Maçã
            4 => 1000, // Melancia
            _ => 100
        };

        // Propriedade utilitária opcional para nomear no HUD ou Logs
        public string Name => Type switch
        {
            0 => "Cereja",
            1 => "Morango",
            2 => "Laranja",
            3 => "Maçã",
            4 => "Melancia",
            _ => "Desconhecida"
        };
    }
}