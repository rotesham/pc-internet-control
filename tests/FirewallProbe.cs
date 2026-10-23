using System;using System.Net.Sockets;
class FirewallProbe {
 static int Main(string[] args){try{if(args.Length!=1)throw new ArgumentException("Test host required");using(var client=new TcpClient()){var pending=client.BeginConnect(args[0],443,null,null);if(!pending.AsyncWaitHandle.WaitOne(4000)){Console.WriteLine("BLOCKED_OR_TIMEOUT");return 2;}client.EndConnect(pending);Console.WriteLine("TCP_CONNECTED");return 0;}}catch(Exception e){Console.WriteLine("NO_CONNECTION: "+e.Message);return 2;}}
}
