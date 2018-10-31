using System;

namespace QuestradeAPI
{

    public class StreamPort
    {
        public int streamPort { get; set; }
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
        public int symbol { get; set; }
        public string symbolId { get; set; }
        public string tier { get; set; } //TODO change to enum
        public double bidPrice { get; set; }
        public int bidSize { get; set; }
        public double askPrice { get; set; }
        public int askSize { get; set; }
        public double lastTradeTrHrs { get; set; }
        public double lastTradePrice { get; set; }
        public int lastTradeSize { get; set; }
        public string lastTradeTick { get; set; } //TODO change to enum
        public int volume { get; set; }
        public double openPrice { get; set; }
        public double highPrice { get; set; }
        public double lowPrice { get; set; }
        public bool delay { get; set; }
        public bool isHalted { get; set; }
    }

    public class Quotes
    {
        Quote[] quotes { get; set; }
    }

    public class Candles
    {
        public Candle[] candles { get; set; }
    }

    public enum SecurityType { Stock, Option, Bond, Right, Gold, MutualFund, Index }

    public enum ListingExch { TSX, TSXV, CNSX, MX, NASDAQ, NYSE, ARCA, OPRA, PINX, OTCBB }

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
}