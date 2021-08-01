namespace MCPromoter
{
    public static class FakePlayerData
    {
        public class List
        {
            public string type { get; set; }
            public ListData data { get; set; }
        }

        public class ListData
        {
            public string[] list { get; set; }
        }

        public class Operation
        {
            public string type { get; set; }
            public OperationData data { get; set; }
        }

        public class OperationData
        {
            public string name { get; set; }
            public bool success { get; set; }
            public string reason { get; set; }
        }
    }
}