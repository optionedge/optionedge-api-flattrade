using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Utf8Json;

namespace OptionEdge.API.FlatTrade.Records
{
    public class ConnectAck
    {
        public ConnectAck(dynamic data)
        {
            try
            {
               
                var responsType = data["t"];
                ResponseType = responsType;

                UserId = data["uid"];

                Status = data["s"];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public string ResponseType { get; set; }
        public string UserId { get; set; }

        public string Status { get; set; }

    }
}
