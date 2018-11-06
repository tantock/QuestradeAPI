using System;

namespace QuestradeAPI
{
    public class StreamPort
    {
        public int streamPort { get; set; }
    }

    public class StreamResponse
    {
        public bool success;
    }

    public class AuthenticateResp
    {
        public string access_token { get; set; }
        public string api_server { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public string token_type { get; set; }
        public DateTime expires_in_date { get; set; }
    }
    
    
    public class APIReturn<T>
    {
        public bool isSuccess;
        public T q_obj;
        public DateTime RateReset;
        public int NumCallsLeft;
    }

    #region Market Data
    

    public class Candle
    {
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public double open { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double close { get; set; }
        public int volume { get; set; }
        public double VWAP { get; set; }
    }

    public class Quote
    {
        public string symbol { get; set; }
        public int symbolId { get; set; }
        public string tier { get; set; } //TODO change to enum
        public double? bidPrice { get; set; }
        public int bidSize { get; set; }
        public double? askPrice { get; set; }
        public int askSize { get; set; }
        public double? lastTradeTrHrs { get; set; }
        public DateTime? lastTradeTime { get; set; }
        public double? lastTradePrice { get; set; }
        public int lastTradeSize { get; set; }
        public TickType? lastTradeTick { get; set; }
        public int volume { get; set; }
        public double? openPrice { get; set; }
        public double? highPrice { get; set; }
        public double? lowPrice { get; set; }
        public bool delay { get; set; }
        public bool isHalted { get; set; }
        public double? VWAP { get; set; }
    }

    public class Quotes
    {
        public Quote[] quotes { get; set; }
    }

    public class Candles
    {
        public Candle[] candles { get; set; }
    }

    public class Symbol
    {
        public string symbol { get; set; }
        public int symbolId { get; set; }
        public string description { get; set; }
        public SecurityType securityType { get; set; }
        public string listingExchange { get; set; }
        public bool isQuotable { get; set; }
        public bool isTradable { get; set; }
        public string currency { get; set; }
    }

    public class Symbols
    {
        public Symbol[] symbols;
    }
    #endregion

    #region Orders and Execution

    public class Order
    {
        public int id { get; set; }
        public string symbol { get; set; }
        public int symbolId { get; set; }
        public int totalQuantity { get; set; }
        public int openQuantity { get; set; }
        public int filledQuantity { get; set; }
        public int canceledQuantity { get; set; }
        public double? limitPrice { get; set; }
        public double? stopPrice { get; set; }
        public bool isAllOrNone { get; set; }
        public bool isAnonymous { get; set; }
        public int? icebergQuantity { get; set; }
        public int? minQuantity { get; set; }
        public double avgExecPrice { get; set; }
        public double? lastExecPrice { get; set; }
        public DateTime? gtdDate { get; set; }
        public string clientReasonStr { get; set; }
        public int chainId { get; set; }
        public DateTime creationTime { get; set; }
        public DateTime upDateTime { get; set; }
        public string notes { get; set; }
        public double commissionCharged { get; set; }
        public string exchangeOrderId { get; set; }
        public bool isSignificantShareholder { get; set; }
        public bool isInsider { get; set; }
        public bool isLimitOffsetInDollar { get; set; }
        public int userId { get; set; }
        public double? placementCommission { get; set; }
        public double? triggerStopPrice { get; set; }
        public int orderGroupId { get; set; }
        public OrderSide side { get; set; }
        public OrderType orderType { get; set; }
        public string source { get; set; }
        public OrderTimeInForce timeInForce { get; set; }
        public OrderState state { get; set; }
        public string primaryRoute { get; set; }
        public string secondaryRoute { get; set; }
        public string orderRoute { get; set; }
        public string venueHoldingOrder { get; set; }
        //public Complex legs{get;set;}-public Complex OrderLeg{get;set;}
        //public Enumeration strategyType{get;set;}
        public OrderClass? orderClass { get; set; }

        //TODO Implement Enum
    }

    public class Orders
    {
        public Order[] orders { get; set; }
    }

    public class Execution
    {
        public string symbol { get; set; }
        public int symbolId { get; set; }
        public int quantity { get; set; }
        public string side { get; set; }
        public double price { get; set; }
        public int id { get; set; }
        public int orderId { get; set; }
        public int orderChainId { get; set; }
        public string exchangeExecId { get; set; }
        public DateTime timestamp { get; set; }
        public string notes { get; set; }
        public string venue { get; set; }
        public double totalCost { get; set; }
        public double orderPlacementCommission { get; set; }
        public double commission { get; set; }
        public double executionFee { get; set; }
        public double secFee { get; set; }
        public int canadianExecutionFee { get; set; }
        public int parentId { get; set; }
    }

    public class Executions
    {
        public Execution[] executions { get; set; }
    }

    #endregion

    #region Accounts
    public class Account
    {
        public AccountType type { get; set; }
        public string number { get; set; }
        public string status { get; set; } 
        public bool isPrimary { get; set; }
        public bool isBilling { get; set; }
        public string clientAccountType { get; set; }
    }

    public class Accounts
    {
        public Account[] accounts { get; set; }
    }
    
    public class AccountBalance
    {
        public Currency currency { get; set; }
        public double cash { get; set; }
        public double marketValue { get; set; }
        public double totalEquity { get; set; }
        public double buyingPower { get; set; }
        public double maintenanceExcess { get; set; }
        public bool isRealTime { get; set; }
    }

    public class AccountBalances
    {
        public AccountBalance[] perCurrencyBalances { get; set; }
        public AccountBalance[] combinedBalances { get; set; }
        public AccountBalance[] sodPerCurrencyBalances { get; set; }
        public AccountBalance[] sodCombinedBalances { get; set; }
    }
    #endregion

    #region Enumerations
    public enum Currency { USD,CAD}
    public enum HistoricalGrandularity { OneMinute, TwoMinutes, ThreeMinutes, FourMinutes, FiveMinutes, TenMinutes, FifteenMinutes, TwentyMinutes, HalfHour, OneHour, TwoHour, FourHour, OneDay, OneWeek, OneMonth, OneYear }


    public enum SecurityType { Stock, Option, Bond, Right, Gold, MutualFund, Index }

    public enum ListingExch { TSX, TSXV, CNSX, MX, NASDAQ, NYSE, ARCA, OPRA, PINX, OTCBB }

    public enum AccountType { Cash, Margin, TFSA, RRSP, SRRSP, LRRSP, LIRA, LIF, RIF, SRIF, LRIF, RRIF, PRIF, RESP, FRESP }
    
    public enum TickType { Up,Down,Equal}

    public enum OrderType { Market, Limit, Stop, StopLimit,TrailStopInPercentage, TrailStopInDollar, TrailStopLimitInPercentage, TrailStopLimitInDollar, LimitOnOpen, LimitOnClose }

    public enum OrderTimeInForce { Day, GoodTillCanceled, GoodTillExtendedDay, GoodTillDate, ImmediateOrCancel, FillOrKill }

    public enum OrderAction { Buy, Sell}

    public enum OrderClass { Primary,Limit,StopLoss}

    public enum OrderSide { Buy, Sell, Short, Cov, BTO, STC, BTC}

    public enum OrderState {Failed, Pending, Accepted, Rejected, CancelPending, Canceled, PartialCanceled, Partial, Executed, ReplacePending, Replaced, Stopped, Suspended, Expired, Queued, Triggered, Activated, PendingRiskReview, ContingentOrder}
   
    #endregion
}