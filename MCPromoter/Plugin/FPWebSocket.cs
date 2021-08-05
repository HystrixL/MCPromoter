using WebSocketSharp;
using static MCPromoter.Output;

namespace MCPromoter
{
    partial class MCPromoter
    {
        public static void BotListener(object sender, MessageEventArgs e)
                {
                    string receive = e.Data;
                    if (receive.Contains("\"type\": \"list\""))
                    {
                        FakePlayerData.List fakePlayerList = javaScriptSerializer.Deserialize<FakePlayerData.List>(receive);
                        string list = string.Join("、", fakePlayerList.data.list);
                        StandardizedFeedback("@a",$"服务器内存在假人 {list}");
                    }
                    else if (receive.Contains("\"type\": \"add\"")||receive.Contains("\"type\": \"remove\"")||receive.Contains("\"type\": \"connect\"")||receive.Contains("\"type\": \"disconnect\""))
                    {
                        FakePlayerData.Operation fakePlayerOperation =
                            javaScriptSerializer.Deserialize<FakePlayerData.Operation>(receive);
                        if (!fakePlayerOperation.data.success)
                        {
                            StandardizedFeedback("@a",$"操作假人{fakePlayerOperation.data.name}失败");
                        }
                    }
                }
    }
    
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