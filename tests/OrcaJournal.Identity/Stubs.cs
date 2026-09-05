using System;
namespace NinjaTrader.Cbi
{
    public enum MarketPosition { Long, Short, Flat }
    public enum OrderAction { Buy, BuyToCover, Sell, SellShort }
    public class Order { public OrderAction OrderAction; }
    public class MasterInstrument { public string Name; }
    public class Instrument { public string FullName; public MasterInstrument MasterInstrument; }
    public class Execution { public Instrument Instrument; public Order Order; public string ExecutionId; public int Quantity; public int Position; public double Price; public DateTime Time; public MarketPosition MarketPosition; }
    public class ConnectionStatusEventArgs : EventArgs {}
    public class Connection
    {
        public static event EventHandler<ConnectionStatusEventArgs> ConnectionStatusUpdate;
        public static void Changed() { ConnectionStatusUpdate?.Invoke(null,new ConnectionStatusEventArgs()); }
    }
    public class ExecutionEventArgs : EventArgs { public bool IsSod; public Execution Execution; }
    public class Account
    {
        public string Name;
        public event EventHandler<ExecutionEventArgs> ExecutionUpdate;
        public void Deliver(Execution execution, bool sod = false) { ExecutionUpdate?.Invoke(this, new ExecutionEventArgs { Execution = execution, IsSod = sod }); }
    }
}
