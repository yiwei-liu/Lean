﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Globalization;
using Newtonsoft.Json;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect
{
    /// <summary>
    /// QuantConnect University: Live Trading Functionality Demonstration:
    /// This algorithm demonstrates the underlying functionality specifically for live trading.
    /// </summary>
    public class LiveTradingFeaturesAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2013, 1, 1);
            SetEndDate(DateTime.Now.Date.AddDays(-1));
            SetCash(25000);

            //Equity Data for US Markets:
            AddSecurity(SecurityType.Equity, "AAPL", Resolution.Second);

            //FOREX Data for Weekends: 24/6
            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Minute);

            //Custom/Bitcoin Live Data: 24/7
            AddData<Bitcoin>("BTC", Resolution.Second);
        }

        //New Bitcoin Data Event:
        public void OnData(Bitcoin data)
        {
            if (LiveMode) //Live Mode Property
            {
                //Configurable title header statistics numbers
                SetRuntimeStatistic("BTC", data.Close.ToString("C"));
            }

            if (!Portfolio.HoldStock)
            {
                Order("BTC", 100);

                //Send a notification email/SMS/web request on events:
                Notify.Email("myemail@gmail.com", "Test", "Test Body", "test attachment");
                Notify.Sms("+11233456789", Time.ToString("u") + ">> Test message from live BTC server.");
                Notify.Web("http://api.quantconnect.com", Time.ToString("u") + ">> Test data packet posted from live BTC server.");
            }
        }

        public void OnData(TradeBars data)
        {
            if (!Portfolio.HoldStock && data.ContainsKey("AAPL"))
            {
                int quantity = (int)Math.Floor(Portfolio.Cash / data["AAPL"].Close);
                Order("AAPL", quantity);
                Debug("Purchased SPY on " + Time.ToShortDateString());
                Notify.Email("myemail@gmail.com", "Test", "Test Body", "test attachment");
            }
        }
    }


    /// <summary>
    /// Custom Data Type: Bitcoin data from Quandl - http://www.quandl.com/help/api-for-bitcoin-data
    /// </summary>
    public class Bitcoin : BaseData
    {
        //Set the defaults:
        /// <summary>
        /// Open Price
        /// </summary>
        public decimal Open = 0;
        
        /// <summary>
        /// High Price
        /// </summary>
        public decimal High = 0;
        
        /// <summary>
        /// Low Price
        /// </summary>
        public decimal Low = 0;

        /// <summary>
        /// Closing Price
        /// </summary>
        public decimal Close = 0;
        
        /// <summary>
        /// Volume in BTC
        /// </summary>
        public decimal VolumeBTC = 0;
        
        /// <summary>
        /// Volume in USD:
        /// </summary>
        public decimal WeightedPrice = 0;

        /// <summary>
        /// Default Constructor Required.
        /// </summary>
        public Bitcoin()
        {
            Symbol = "BTC";
        }

        /// <summary>
        /// Source URL's of Backtesting and Live Streams:
        /// </summary>
        public override string GetSource(SubscriptionDataConfig config, DateTime date, DataFeedEndpoint datafeed)
        {
            var source = "";

            switch (datafeed)
            {
                //Historical backtesting data:
                case DataFeedEndpoint.FileSystem:
                case DataFeedEndpoint.Backtesting:
                    source = "http://www.quandl.com/api/v1/datasets/BITCOIN/BITSTAMPUSD.csv?sort_order=asc";
                    break;

                //Live socket for bitcoin prices:
                case DataFeedEndpoint.LiveTrading:
                    //Live refreshing endpoint.
                    source = "https://www.bitstamp.net/api/ticker/";
                    break;
            }

            System.Console.WriteLine(DateTime.Now.ToString("u") + " SOURCE >> " + source);

            return source;
        }

        /// <summary>
        /// Backtesting &amp; Live Bitcoin Decoder:
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint datafeed)
        {
            Bitcoin coin = new Bitcoin();
            switch (datafeed)
            {
                //Example Line Format:
                //Date      Open   High    Low     Close   Volume (BTC)    Volume (Currency)   Weighted Price
                //2011-09-13 5.8    6.0     5.65    5.97    58.37138238,    346.0973893944      5.929230648356
                case DataFeedEndpoint.FileSystem:
                case DataFeedEndpoint.Backtesting:
                    try
                    {
                        string[] data = line.Split(',');
                        coin.Time = DateTime.Parse(data[0]);
                        coin.Open = Convert.ToDecimal(data[1], CultureInfo.InvariantCulture);
                        coin.High = Convert.ToDecimal(data[2], CultureInfo.InvariantCulture);
                        coin.Low = Convert.ToDecimal(data[3], CultureInfo.InvariantCulture);
                        coin.Close = Convert.ToDecimal(data[4], CultureInfo.InvariantCulture);
                        coin.VolumeBTC = Convert.ToDecimal(data[5], CultureInfo.InvariantCulture);
                        coin.WeightedPrice = Convert.ToDecimal(data[7], CultureInfo.InvariantCulture);
                        coin.Symbol = "BTC";
                        coin.Value = coin.Close;
                    }
                    catch { /* Do nothing, skip first title row */ }
                    break;

                //Example Line Format:
                //{"high": "441.00", "last": "421.86", "timestamp": "1411606877", "bid": "421.96", "vwap": "428.58", "volume": "14120.40683975", "low": "418.83", "ask": "421.99"}
                case DataFeedEndpoint.LiveTrading:
                    try
                    {
                        var liveBTC = JsonConvert.DeserializeObject<LiveBitcoin>(line);
                        coin.Time = DateTime.Now;
                        coin.Open = liveBTC.Last;
                        coin.High = liveBTC.High;
                        coin.Low = liveBTC.Low;
                        coin.Close = liveBTC.Last;
                        coin.VolumeBTC = liveBTC.Volume;
                        coin.WeightedPrice = liveBTC.VWAP;
                        coin.Symbol = "BTC";
                        coin.Value = coin.Close;
                    }
                    catch { /* Do nothing, possible error in json decoding */ }
                    break;
            }

            System.Console.WriteLine(DateTime.Now.ToString("u") + " READER >> " + line + " COIN >> " + coin.Time.ToString("u"));

            return coin;
        }
    }

    /// <summary>
    /// Live data structure
    /// </summary>
    public class LiveBitcoin
    {
        public int Timestamp = 0;
        public decimal Last = 0;
        public decimal High = 0;
        public decimal Low = 0;
        public decimal Bid = 0;
        public decimal Ask = 0;
        public decimal VWAP = 0;
        public decimal Volume = 0;
    }
}