#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	
	
	
	
	/// <summary>
	/// MasterAirOne - A trading strategy that uses ATR Trailing Stops for entry and exit signals
	/// This strategy implements a trend-following approach using multiple ATR-based trailing stops
	/// </summary>
	public class MimicV2 : Strategy
	{
		// Trading state variables
		private int LT;              // Long Trail value
		private int ST;              // Short Trail value
		private double newPrice;     // Current price for long positions
		private double newPrice2;    // Current price for short positions
		private bool inLong;         // Flag indicating if in long position
		private bool inShort;        // Flag indicating if in short position
		private double trailOffset;  // Offset for trailing stop calculations

		// ATR Trailing Stop indicators
		private ATRTrailingStop ATRTrailingStop1;  // Primary ATR trailing stop
		private ATRTrailingStop ATRTrailingStop2;  // Secondary ATR trailing stop
		private ATRTrailingStop ATRTrailingStop3;  // Tertiary ATR trailing stop
		private ATRTrailingStop ATRTrailingStop4;  // Quaternary ATR trailing stop

		// Price movement tracking variables
		private double lastHigh;     // Highest price in current move
		private double lastLow;      // Lowest price in current move
		private double lastMove;     // Last price movement
		private double accumulativeMove;  // Cumulative price movement
		private double pastThreeMoves;    // Average of past three moves
		private double price1;       // Price reference point 1
		private double price2;       // Price reference point 2
		private bool enableTrading;  // Flag to enable/disable trading

		// Trend direction flags
		private bool movingHigher;   // Flag indicating upward trend
		private bool movingLower;    // Flag indicating downward trend
		private bool checkMovesEnabled;  // Flag to enable move checking

		// Position tracking
		private double inLongPrice;  // Entry price for long positions
		private double inShortPrice; // Entry price for short positions
		private double p1;           // Price reference 1
		private double p2;           // Price reference 2

		// Track What bar is being used
		private bool boolCharBar;
		private bool boolAddedSeries;
		
		// Tracking if the first bar of added series has printed
		private bool boolFirstBarPrinted;
		
		// First bar parameters
		private double fbOpen;
		private double fbClose;
		private double fbHigh;
		private double fbLow;
		
		private double triggerStop;
		private double triggerTP;
		
		private bool goLong;
		private bool goShort;
		
		private bool hitTPTrigger;
		
		private bool stopInProfitTrigger;
		private double stopInProfitPrice;
		
		private Brush myBrush;
		private Brush myBrush2;
		private int myBarCount;
		
		#region autobot
		private double tickValue;
		private int maxTickLoss;
		#endregion
		
		private int Contracts;
		#region RiskLogic
		// Risk management variables
		private int priorTradesCount;        // Count of previous trades
		private double priorTradesCumProfit; // Cumulative profit from previous trades
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"3 atr trail";
				Name										= "MimicV2";
				
				IsOverlay     = true;
				
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				
				StartTime						= DateTime.Parse("07:30", System.Globalization.CultureInfo.InvariantCulture);
				EndTime						= DateTime.Parse("11:00", System.Globalization.CultureInfo.InvariantCulture);
			
				
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= false;
				Trailpercent					= 6.8;
				Whick					= 3.6;
				TrendTrail					= 13.6;
				Ltrail					= -14;
				Strail					= 14;
				
				LT					= 0;
				ST					= 0;
			
				AddPlot(Brushes.Yellow, "AtrCross");
				AddPlot(Brushes.Gray, "AtrCross2");
				inLong = false;
				inShort = false;
				
				
				
				trailOffset = 0;
				
				
				longOn = true;
				shortOn = true;
				lastHigh = 0;
				lastLow = 0;
				lastMove = 0;
				accumulativeMove = 0;
				pastThreeMoves = 0;
				price1 = 0;
				price2 = 0;
				movingHigher = false;
				movingLower = false;
				Contracts = 1;
				
				//These two work together.  Set CheckMoves to true and enableTrading to false to test
				checkMovesEnabled = false;
				enableTrading = true;
				
				inLongPrice = 0;
				inShortPrice = 0;
				
				
				barPeriodType1				= tc3PeriodTypes.Minute;
				barPeriodValue1				= 30;
				
				boolCharBar = false;
				boolAddedSeries = false;
				boolFirstBarPrinted = false;
				
				fbOpen = 0;
				fbClose = 0;
				fbHigh = 0;
				fbLow = 0;
				
				StopPercent = 100;
				TPPercent = 50;
				
				triggerStop = 0;
				triggerTP = 0;
				
				goLong = false;
				goShort = false;
				tradeSpecialCase = true;
				hitTPTrigger = false;
				UseStopInProfit = false;
				StopInProfitAddTicks = 0;
				stopInProfitTrigger = false;
				stopInProfitPrice = 0;
				TradeEveryCandle = false;
				myBarCount = 1;
				
				ShowCandleLine = true;
				ShowFullCandle = true;
				BarOpacity = 30;
				
				CandleColor =  Brushes.PaleGreen;	 
				CandleColor2 = Brushes.PaleVioletRed;
				
				#region autobot
				MaxContracts = 100;
				MaxLoss = 200;
				
				MinContracts = 1;
				#endregion
				
				#region RiskLogic
				ProfitStop = 100;
				LossStop = 100;
				MaxTrades = 500;
				priorTradesCount = 0;
				priorTradesCumProfit = 0;
				#endregion
			
				
				
			}
			else if (State == State.Configure)
			{
				myBrush = new SolidColorBrush(Color.FromArgb(50, 47, 229, 138));
				myBrush.Freeze();
				myBrush2 = new SolidColorBrush(Color.FromArgb(50, 245, 0, 147));
				myBrush2.Freeze();
				AddDataSeries((BarsPeriodType)barPeriodType1, barPeriodValue1);	//	[1]
			}
			else if (State == State.DataLoaded)
			{				
				ATRTrailingStop1				= ATRTrailingStop(Close, 1, Whick);
				ATRTrailingStop2				= ATRTrailingStop(Close, 1, Trailpercent);
				ATRTrailingStop3				= ATRTrailingStop(Close, 1, TrendTrail);
				ATRTrailingStop4				= ATRTrailingStop(Close, 1, TrendTrail);
				ATRTrailingStop1.Plots[0].Brush = Brushes.Red;
				ATRTrailingStop2.Plots[0].Brush = Brushes.Lime;
				ATRTrailingStop3.Plots[0].Brush = Brushes.DodgerBlue;
				AddChartIndicator(ATRTrailingStop1);
				AddChartIndicator(ATRTrailingStop2);
				AddChartIndicator(ATRTrailingStop3);
				lastLow = Low[0];
				lastHigh = High[0];
				
				NinjaTrader.Gui.Tools.SimpleFont mf = new NinjaTrader.Gui.Tools.SimpleFont("Consolas", 20) { Size = 20, Bold = true };
				TextFixed TFv = Draw.TextFixed(this, "Version", "Mimic V2", TextPosition.BottomRight, Brushes.Aquamarine, mf, Brushes.Transparent, Brushes.Transparent, 0);
			}
			// Once the NinjaScript object has reached State.Historical, our custom control can now be added to the chart
		   else if (State == State.Historical)
		   {
			  
		    
		  }
		    // When NinjaScript object is removed, make sure to unsubscribe to button click events
		  else if (State == State.Terminated)
		  {
			 
		    
		  }
		  else if (State == State.Realtime)
			{
				ExitShort();
				ExitLong();
				boolFirstBarPrinted = false;	
				hitTPTrigger = false;
				#region RiskLogic
				// Store the strategy's prior cumulated realized profit and number of trades
				priorTradesCount = SystemPerformance.AllTrades.Count;
				priorTradesCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
				#endregion
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 0) //Chart Bars
			{
				boolCharBar = true;
				boolAddedSeries = false;
				
			}
			else if(BarsInProgress == 1) //Added Series
			{
				boolCharBar = false;
				boolAddedSeries = true;
				if(boolFirstBarPrinted) //after the first bar prints we do not need to check the series
				{
					if(!TradeEveryCandle){
						Print("BarsInProgress = 1 so return");
						return;
					}
					else
					{
						Print("Keep Trading");	
						//Draw.VerticalLine(this, @"", 0, Brushes.Lime, DashStyleHelper.Solid, 2);
						int barsAgo =CurrentBars[0]- myBarCount;
						int halfBarsAgo = barsAgo/2;
						//Print("Bars Ago: "+ barsAgo.ToString());
						if(Closes[1][0] >= Opens[1][0])
						{
							//ShowCandleLine = true;
							//ShowFullCandle = true;
							if(ShowCandleLine){
								Draw.VerticalLine(this, @"New Candle" + Convert.ToString(CurrentBar), 0, myBrush, DashStyleHelper.Solid, 4);
							}
							if(ShowFullCandle){
								//Draw.Rectangle(this, "Candle Region" + Convert.ToString(CurrentBar), CurrentBar - myBarCount, Opens[1][0], 0, Closes[1][0], myBrush);
								Draw.Rectangle(this, "tag1" + Convert.ToString(CurrentBar), false,barsAgo, Opens[1][0], 0, Closes[1][0], CandleColor, CandleColor, BarOpacity);
								Draw.Rectangle(this, "tag2" + Convert.ToString(CurrentBar), false,halfBarsAgo-1, Lows[1][0], halfBarsAgo+1, Highs[1][0], Brushes.White, Brushes.White, BarOpacity);
								//Draw.Rectangle(this, "tag3" + Convert.ToString(CurrentBar), false,halfBarsAgo-1, Opens[1][0], halfBarsAgo+1, Lows[1][0], Brushes.White, Brushes.White, 30);
							}
						}
						else
						{
							if(ShowCandleLine){
								Draw.VerticalLine(this, @"New Candle" + Convert.ToString(CurrentBar), 0, myBrush2, DashStyleHelper.Solid, 4);
							}
							if(ShowFullCandle){
								//Draw.Rectangle(this, "Candle Region" + Convert.ToString(CurrentBar), CurrentBar - myBarCount, Opens[1][0], 0, Closes[1][0], myBrush2);
								Draw.Rectangle(this, "tag1" + Convert.ToString(CurrentBar), false,barsAgo, Opens[1][0], 0, Closes[1][0], CandleColor2, CandleColor2, BarOpacity);
								Draw.Rectangle(this, "tag2" + Convert.ToString(CurrentBar), false,halfBarsAgo-1, Lows[1][0], halfBarsAgo+1, Highs[1][0], Brushes.White, Brushes.White, BarOpacity);
								//Draw.Rectangle(this, "tag3" + Convert.ToString(CurrentBar), false,halfBarsAgo-1, Opens[1][0], halfBarsAgo+1, Lows[1][0], Brushes.White, Brushes.White, 30);
							}
						}
						myBarCount = CurrentBars[0];
					}
					
				}
				
			}
			else
			{
				Print("BarsInProgress not 0 or 1 so return");
				return; //if not the chart bars	
			}
			
			
			if (CurrentBars[0] < 1){
				Print("CurrentBars[0] < 1 so return");
				return;
				
			}
			
			if (CurrentBars[1] < 1)
			{
				Print("CurrentBars[1] < 1 so return");
				return;
			}
			
			
			SetVarPriceMoves();
			
			//Set Vars
			SetVarsOnCrosses();
			
			#region RiskLogic
			if (Bars.IsFirstBarOfSession)
			{
				// Store the strategy's prior cumulated realized profit and number of trades
				priorTradesCount = SystemPerformance.AllTrades.Count;
				priorTradesCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;

				/* NOTE: Using .AllTrades will include both historical virtual trades as well as real-time trades.
				If you want to only count profits from real-time trades please use .RealtimeTrades. */
			}

			/* Prevents further trading if the current session's realized profit exceeds $1000 or if realized losses exceed $400.
			Also prevent trading if 10 trades have already been made in this session. */
			if (SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit >= ProfitStop
				|| SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit <= -LossStop
				|| SystemPerformance.AllTrades.Count - priorTradesCount >= MaxTrades)
			{
				/* TIP FOR EXPERIENCED CODERS: This only prevents trade logic in the context of the OnBarUpdate() method. If you are utilizing
				other methods like OnOrderUpdate() or OnMarketData() you will need to insert this code segment there as well. */
				Print("Stop trade Profit: " +(SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit).ToString());
				Print("Stop Num Trades: " +(SystemPerformance.AllTrades.Count - priorTradesCount).ToString());
				//
				// Returns out of the OnBarUpdate() method. This prevents any further evaluation of trade logic in the OnBarUpdate() method.
				return;
			}
			
			
			
			#endregion
			//if not between start and end time return
			if(!((Times[0][0].TimeOfDay > StartTime.TimeOfDay)
				 && (Times[0][0].TimeOfDay < EndTime.TimeOfDay)))
			{
				//Print("Don't Trade Time of Day ");
				boolFirstBarPrinted = false;
				return;
				
			}
			
			//Should be first bar in added series after time start
			if(BarsInProgress == 1) //Added Series  
			{
				SetInitCandle();
				
			}
			
		}
		
		//this should only be called one time to set the vars of the init candle from the added data series
		//When boolFirstBarPrinted is true it will return from above code on series[1]
		private void SetInitCandle()
		{
			Print("SetInitCandle");
			fbOpen = Opens[1][0];
			fbClose = Closes[1][0];
			fbHigh = Highs[1][0];
			fbLow = Lows[1][0];
				
			Print("fbOpen: " + fbOpen.ToString());
			Print("fbClose: " + fbClose.ToString());
			Print("fbHigh: " + fbHigh.ToString());
			Print("fbLow: " + fbLow.ToString());
			//triggerStop = 0;
			//triggerTP = 0;
			
		
			
			
			if(fbClose >= fbOpen) //Green bar or doge
			{
				if(((fbHigh - fbClose) > (fbClose - fbLow))&& tradeSpecialCase)//Big head wick so we go short instead
				{	
					Print("Special Case Green bar but go short: ");
					goLong = false;//TODO: Dont need 2 vars for this
					goShort = true;
					
				}
				else
				{
					Print("Green bar but go long: ");
					goLong = true;
					goShort = false;
				}
			}
			else //Red Bar
			{
				if(((fbClose - fbLow) > (fbHigh - fbClose  )) && tradeSpecialCase)//Big tail wick so we go long instead
				{
					Print("Special Case Red bar but go long: ");
					goLong = true;
					goShort = false;
					
				}
				else
				{
					Print("Red bar but go Short: ");
					goLong = false;
					goShort = true;
				}
				
			}
			if((Position.MarketPosition == MarketPosition.Flat))
			{
				if(goLong)
				{
					Print("goLong");
					double canSize = fbClose - 	fbLow;
					triggerStop = fbClose - canSize*(StopPercent/100);
					triggerTP = fbClose + canSize*(TPPercent/100);
					
					if(longOn /*&& enableTrading*/)//Dont need enableTrading because that is set true on 3 moves higher and we dont use that
					{
						Print("Enter Long");
						#region autobot
						double priceMove = (fbClose - triggerStop)*Bars.Instrument.MasterInstrument.PointValue;
						Print("priceMove: " + priceMove.ToString());
						if((priceMove > MaxLoss))
						{
							if( MinContracts == 0)
							{
								Print("No trade because MinContracts 0 Not safe trade");
								Draw.VerticalLine(this, @"NoTrade Line" + Convert.ToString(CurrentBar), 0, Brushes.White);
								return;
							}
							Print("priceMove > MaxLoss ");
							priceMove = MaxLoss;	
						}
						//(int)Math.Round(MaxLoss/tickValue);
						int numContracts = (int)Math.Round(MaxLoss/priceMove);
						Print("First numContracts:  " + numContracts.ToString());
						
						if(numContracts < 1)
						{
							numContracts =1;	
						}
						if(numContracts > MaxContracts)
						{
							numContracts = MaxContracts;
						}
						if(numContracts < MinContracts)
						{
							numContracts = MinContracts;
						}
						//EnterLong(numContracts,"goLong");//Go Long
						Contracts = numContracts;
						#endregion
						Print("Last numContracts:  " + numContracts.ToString());
						EnterLong(numContracts, "longEntry");
						inLongPrice = Close[0];
						//stopInProfitPrice = inLongPrice + (StopInProfitAddTicks*Bars.Instrument.MasterInstrument.TickSize);
						//Position.AveragePrice
						stopInProfitPrice = Position.AveragePrice + (StopInProfitAddTicks*Bars.Instrument.MasterInstrument.TickSize);
						Print("stopInProfitPrice: " + stopInProfitPrice.ToString()); 
						if(UseStopInProfit)
						{
							stopInProfitTrigger = false;
						}
							//UseStopInProfit = false;
							//StopInProfitAddTicks = 0;
							//stopInProfitTrigger = false;
							//stopInProfitPrice = 0;
						
						inLong = true;
						hitTPTrigger = false;
					}
					
					Draw.VerticalLine(this, @"Long Line" + Convert.ToString(CurrentBar), 0, Brushes.Green);
					Draw.HorizontalLine(this, @"TP Line" , triggerTP, Brushes.Green);	
					Draw.HorizontalLine(this, @"SL Line" , triggerStop, Brushes.Red);	
				}
				else
				{
					Print("goShort");
					double canSize = fbHigh - fbClose;
					triggerStop = fbClose + canSize*(StopPercent/100);
					triggerTP = fbClose - canSize*(TPPercent/100);
					
					if(shortOn /*&& enableTrading*/)//Dont need enableTrading because that is set true on 3 moves higher and we dont use that
					{
						Print("enterShort");
						#region autobot
						double priceMove = ( triggerStop - fbClose)*Bars.Instrument.MasterInstrument.PointValue;
						Print("priceMove: " + priceMove.ToString());
						if((priceMove > MaxLoss))
						{
							if( MinContracts == 0)
							{
								Print("No trade because MinContracts 0 Not safe trade");
								Draw.VerticalLine(this, @"NoTrade Line" + Convert.ToString(CurrentBar), 0, Brushes.White);
								return;
							}
							Print("priceMove > MaxLoss ");
							priceMove = MaxLoss;	
						}
						//(int)Math.Round(MaxLoss/tickValue);
						int numContracts = (int)Math.Round(MaxLoss/priceMove);
						Print("First numContracts:  " + numContracts.ToString());
						if(numContracts < 1)
						{
							numContracts =1;	
						}
						if(numContracts > MaxContracts)
						{
							numContracts = MaxContracts;
						}
						if(numContracts < MinContracts)
						{
							numContracts = MinContracts;
						}
						Contracts = numContracts;
						//EnterLong(numContracts,"goLong");//Go Long
						#endregion
						Print("Last numContracts:  " + numContracts.ToString());
						EnterShort(numContracts, "shortEntry");
						inShortPrice = Close[0];
						//stopInProfitPrice = inShortPrice - (StopInProfitAddTicks*Bars.Instrument.MasterInstrument.TickSize);
						stopInProfitPrice = Position.AveragePrice - (StopInProfitAddTicks*Bars.Instrument.MasterInstrument.TickSize);
						Print("stopInProfitPrice: " + stopInProfitPrice.ToString()); 
						if(UseStopInProfit)
						{
							stopInProfitTrigger = false;
						}
							//UseStopInProfit = false;
							//StopInProfitAddTicks = 0;
							//stopInProfitTrigger = false;
							//stopInProfitPrice = 0;
						inShort = true;
						hitTPTrigger = false;
					}
					
					Draw.VerticalLine(this, @"Short Line" + Convert.ToString(CurrentBar), 0, Brushes.Red);
					Draw.HorizontalLine(this, @"TP Line" , triggerTP, Brushes.Green);	
					Draw.HorizontalLine(this, @"SL Line" , triggerStop, Brushes.Red);	
					
				}
				// Sets the bar color to yellow
				BarBrush = Brushes.Yellow;	
				
				boolFirstBarPrinted = true;	
				Print("TriggerTP: " + triggerTP.ToString());
				Print("TriggerStop: " + triggerStop.ToString());
			}
		}
		private void SetVarPriceMoves()
		{
			AtrCross[0] = newPrice;
			AtrCross2[0] = newPrice2;
			
			if(High[0] > lastHigh)
			{
				lastHigh = High[0]; 
			}
			
			if(Low[0] < lastLow)
			{
				lastLow = Low[0]; 
			}
			
		
			if(checkMovesEnabled)
			{
				if(movingHigher)
				{
					if((High[0] - lastLow) > Math.Abs(pastThreeMoves))
					{
						enableTrading = true;
					}
					else
					{
						enableTrading = false;
					}
				}
				else
				{
					if((lastHigh - Low[0]) > Math.Abs(pastThreeMoves))
					{
						enableTrading = true;
					}
					else
					{
						enableTrading = false;
					}
					
				}
			}
		
		}
	
		//Set different vars on crosses
		private void SetVarsOnCrosses()
		{		
				
			//Check if the stop loss is hit
			if(inLong) 
			{
				//if(Close[0] >= triggerTP)
				if(AtrCross[0] >= triggerTP)
				{
					Print("In Long hit TP Trigger");
					hitTPTrigger = true;
				}
				
				if(Close[0] <= triggerStop)
				{
					//ExitLong(Contracts, "", "");
					ExitLong();
					inLong = false;
					trailOffset = 0;
					hitTPTrigger = false;
					
				}
				//UseStopInProfit = false;
							//StopInProfitAddTicks = 0;
							//stopInProfitTrigger = false;
							//stopInProfitPrice = 0;
				if(UseStopInProfit)
				{
					if((Close[0] >= triggerTP)&& !stopInProfitTrigger)
					{
						stopInProfitTrigger = true;
						Print("Set ExitLongStopMarket: " + stopInProfitPrice.ToString());
						Draw.HorizontalLine(this, @"Stop IP Line" , stopInProfitPrice, Brushes.Yellow);	
						//ExitLongStopMarket(stopInProfitPrice);
						//ExitLongStopLimit(triggerTP,stopInProfitPrice);
					}
					if((Close[0] <= stopInProfitPrice) && stopInProfitTrigger)
					{
						//ExitLong(Contracts, "", "");
						RemoveDrawObject(@"Stop IP Line");
						ExitLong();
						inLong = false;
						trailOffset = 0;
						hitTPTrigger = false;
						stopInProfitTrigger = false;
					}
				}
				
			}
			
			if(inShort) 
			{
				//if(Close[0] <= triggerTP)
				if(AtrCross2[0] <= triggerTP)
				{
					//Print("In Short hit TP Trigger");
					hitTPTrigger = true;
				}
				
				if(Close[0] >= triggerStop)
				{
					//ExitShort(Contracts, "", "");
					ExitShort();
					inShort = false;
					trailOffset = 0;
					hitTPTrigger = false;
					
				}
				if(UseStopInProfit)
				{
					if((Close[0] <= triggerTP)&& !stopInProfitTrigger)
					{
						stopInProfitTrigger = true;
						Print("Set ExitLongStopMarket: " + stopInProfitPrice.ToString());
						Draw.HorizontalLine(this, @"Stop IP Line" , stopInProfitPrice, Brushes.Yellow);	
						//ExitShortStopLimit(triggerTP, stopInProfitPrice);
					}
					if((Close[0] >= stopInProfitPrice) && stopInProfitTrigger)
					{
						//ExitLong(Contracts, "", "");
						RemoveDrawObject(@"Stop IP Line");
						ExitShort();
						inShort = false;
						trailOffset = 0;
						hitTPTrigger = false;
						stopInProfitTrigger = false;
					}
				}
			}
			
			 // Set 1
			if (CrossAbove(Close, ATRTrailingStop1, 1))
			{
				LT = Convert.ToInt32((Close[0] + (Ltrail * TickSize)) );
				//Draw.Ray(this, @"Long Ray_1 " + Convert.ToString(Close[0]), false, 1, (Close[0] + (Ltrail * TickSize)) , 0, (Close[0] + (Ltrail * TickSize)) , Brushes.CornflowerBlue, DashStyleHelper.Solid, 2);
				
				//this makes it only go higher and not drop back down when in a trade
				if(inLong)
				{
					if((Close[0] + (Ltrail * TickSize) + trailOffset) > newPrice)
					{
						newPrice = Close[0] + (Ltrail * TickSize) + trailOffset;
							
					}
				}
				else
				{
					newPrice = Close[0] + (Ltrail * TickSize);
				}
				
				
				
				AtrCross[0] = newPrice;
				
				//Draw.VerticalLine(this, Close[0] + "tag2", 0, Brushes.Red);
			}
			
			
			
			
			 // Set 3
			if (CrossBelow(Close, ATRTrailingStop1, 1))
			{
				ST = Convert.ToInt32((Close[0] + (Strail * TickSize)) );
				//Draw.Ray(this, @"Short Ray_1 " + Convert.ToString(Close[0]), false, 1, (Close[0] + (Strail * TickSize)) , 0, (Close[0] + (Strail * TickSize)) , Brushes.CornflowerBlue, DashStyleHelper.Solid, 2);
				//newPrice2 = Close[0] + (Strail * TickSize);
				
				if(inShort)
				{
					if((Close[0] + (Strail * TickSize)- trailOffset) < newPrice2)
					{
						newPrice2 = Close[0] + (Strail * TickSize)- trailOffset;
						
						
						
					}
				}
				else
				{
					newPrice2 = Close[0] + (Strail * TickSize);
				}
				
				
				AtrCross2[0] = newPrice2 ;
				//Draw.VerticalLine(this, Close[0] + "tag1", 0, Brushes.White);
			
			}
		
			
			if(hitTPTrigger)//if we hit our TPTrigger we will close on the trailing
			{
				if (CrossBelow(Close, AtrCross, 1))
				{
					//ExitLong(Contracts, "", "");
					ExitLong();
					inLong = false;
					
					trailOffset = 0;
				}
				if (CrossAbove(Close, AtrCross2, 1))
				{
					//ExitShort(Contracts, "", "");
					ExitShort();
					inShort = false;
					
					trailOffset = 0;
				}
			}
		}
		// Supported period types for FVG detection
	    public enum tc3PeriodTypes
	    {
	        Tick = BarsPeriodType.Tick,
	        Volume = BarsPeriodType.Volume,
	        Second = BarsPeriodType.Second,
	        Minute = BarsPeriodType.Minute,
	        Day = BarsPeriodType.Day,
	        Week = BarsPeriodType.Week,
	        Month = BarsPeriodType.Month,
	        Year = BarsPeriodType.Year
	    }
		
			#region Results Stats Box
		
			private double 		NP					= 0;
			private double 		GP					= 0;
			private double		GL					= 0;
			private double 		W					= 0;
			private double 		L					= 0;
			private double      PF					= 0;
			private double      WR					= 0;
			private int    		NT 					= 0;
			
			private int myTradeCount = 0;
			
			protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
			{
				if (State != State.Historical && Position.MarketPosition == MarketPosition.Flat && SystemPerformance.RealTimeTrades.Count > 0 && myTradeCount != SystemPerformance.RealTimeTrades.Count)
				{
					if(DrawObjects["PnLReport"] != null)
					{
					   RemoveDrawObject("PnLReport");
					}
					
					myTradeCount = SystemPerformance.RealTimeTrades.Count-1;
					Trade lastTrade = SystemPerformance.RealTimeTrades[SystemPerformance.RealTimeTrades.Count - 1];
					double totalProfit = SystemPerformance.RealTimeTrades.TradesPerformance.GrossProfit;
					if(lastTrade.ProfitCurrency>0)
					{
						W				= W+1;
						GP				=GP+(lastTrade.ProfitCurrency*Contracts);
					}
					else
					{
						L				= L+1;
						GL				= GL+(lastTrade.ProfitCurrency*Contracts);
					}
						
						NP				= SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit;
						PF				= Math.Round(GP/-GL, 2);
						NT				= NT+1;
						WR				= Math.Round((100*W/NT), 2);	
					
					var Title 			=	string.Format("{0,-10}| ", "NP $")+
					    					string.Format("{0,-10}| ", "PF  ")+
											string.Format("{0,-10}| ", "NT #")+
											string.Format("{0,-10} ", "WR %");

					var Values 			=	string.Format("{0,-10}| ", NP)+
					    					string.Format("{0,-10}| ", PF)+
											string.Format("{0,-10}| ", NT)+
											string.Format("{0,-10} ", WR);

					System.Windows.Media.Brush StatsBrush = System.Windows.Media.Brushes.White;
					
					NinjaTrader.Gui.Tools.SimpleFont myFont2 = new NinjaTrader.Gui.Tools.SimpleFont("Consolas", 20) { Size = 20, Bold = true };
					
					//if(Show_Stats_Box)
					//{
						TextFixed TF = Draw.TextFixed(this, "Stats", Title+"\n"+Values, TextPosition.BottomLeft, ChartControl.Properties.ChartText, myFont2, StatsBrush, StatsBrush, 16);
						TF.OutlineStroke.Width = 1.0f;
					//}
					
					

				}
			}		
			
		#endregion
						
		#region Current RealTime PNL	
			
			private bool 	RunOneTime 				= true;
			private double  InitialBalance 			= 0;
			
			private double  InitialPnL 				= 0; 
			private double  CurrentUnrelizedTotal 	= 0;
			private double  CurrentRelizedTotal 	= 0;
			
			protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
			{
				if(State != State.Historical)
				{
					Account a = Account.All.First(t => t.Name == Account.Name.ToString());

					//Getting The Current Account Balance
					if(RunOneTime){
						InitialPnL = 0;
						RunOneTime = false;
					}
					
					
				    // Print some data to the Output window
					
						CurrentUnrelizedTotal = 0;
						CurrentRelizedTotal	  = 0;
					
						if(Position.MarketPosition != MarketPosition.Flat)	
							CurrentUnrelizedTotal = a.GetAccountItem(AccountItem.UnrealizedProfitLoss, Currency.UsDollar).Value;
						
						CurrentRelizedTotal	  = NP;
					
						double CurrentPnL = CurrentUnrelizedTotal+CurrentRelizedTotal;

						double ReachedPnL	  = CurrentPnL-InitialPnL;
					

					
						var Title2 			=	string.Format("{0,-15}", "Current PnL $");

						var Values2 		=	string.Format("{0,-15}", Math.Round(CurrentUnrelizedTotal,2));

						System.Windows.Media.Brush StatsBrush = System.Windows.Media.Brushes.White;
						if(Math.Round(CurrentUnrelizedTotal,2)>=0)  StatsBrush = Brushes.Lime;
						if(Math.Round(CurrentUnrelizedTotal,2)<0)   StatsBrush = Brushes.Red;
				
						NinjaTrader.Gui.Tools.SimpleFont myFont2 = new NinjaTrader.Gui.Tools.SimpleFont("Consolas", 20) { Size = 20, Bold = false };

						
							TextFixed TF = Draw.TextFixed(this, "PnL", Title2+"\n"+Values2, TextPosition.TopRight, ChartControl.Properties.ChartText, myFont2, StatsBrush, StatsBrush, 8);
							TF.OutlineStroke.Width = 1.0f;											
				

						
					
						if(((CurrentPnL>=ProfitStop) || (CurrentPnL<=-LossStop)) )
						{							
							
							string PnLReport = "";
							if(CurrentPnL>=ProfitStop) PnLReport = "Profit Limit Reached: "+CurrentPnL+"\n(Trading is Not Allowed until next day)";
							if(CurrentPnL<=-LossStop)  PnLReport = "Loss Limit Reached: "+CurrentPnL+"\n(Trading is Not Allowed until next day)";
							Print("HIT TARGET STOP OR PROFIT EXIT TRADES");
						    if(Position.MarketPosition == MarketPosition.Long)
						    {
								
								ExitLong();
						    }		
							else if (Position.MarketPosition == MarketPosition.Short)
							{
								
								ExitShort();
							}
							
							//ResettingObjects();
								
							///Reporting Reached Limit G/L	
						
								TextFixed TF2 = Draw.TextFixed(this, "PnLReport", "\n\n"+PnLReport, TextPosition.TopRight, ChartControl.Properties.ChartText, myFont2, Brushes.Transparent, Brushes.Transparent, 0);	
							
						}				
				}

			}
		
		#endregion		
		
		

		#region Properties
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> AtrCross
		{
			get { return Values[0]; }
		}
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> AtrCross2
		{
			get { return Values[1]; }
		}
	
		#region autobot
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Max Loss", Order=1, GroupName="Contracts")]
		public double MaxLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Max Contracts", Order=2, GroupName="Contracts")]
		public int MaxContracts
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Min Contracts", Order=9, GroupName="Contracts")]
		public int MinContracts
		{ get; set; }
		#endregion
		
		//[Display(ResourceType = typeof(Custom.Resource), Name = "barPeriodType1", GroupName = "00. Trade Candle", Order = 2)]
		//public BarsPeriodType barPeriodType1 { get; set; }

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "CandleColor Green", GroupName = "00. Trade Candle", Order = 530)]
        public Brush CandleColor { get; set; }
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "CandleColor Red", GroupName = "00. Trade Candle", Order = 540)]
        public Brush CandleColor2 { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "barPeriodValue1", GroupName = "00. Trade Candle", Order = 200)]
		public int barPeriodValue1 { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "barPeriodType1", Order = 100, GroupName = "00. Trade Candle")]
        public tc3PeriodTypes barPeriodType1
        { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Candle Line",  Order = 500, GroupName = "00. Trade Candle")]
		public bool ShowCandleLine
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Full Candle",  Order = 510, GroupName = "00. Trade Candle")]
		public bool ShowFullCandle
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Bar Opacity", Order = 520, GroupName = "00. Trade Candle")]
		public int BarOpacity 
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Stop Percent", Order=100, GroupName="01. Candle Triggers")]
		public double StopPercent
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="TP Percent", Order=200, GroupName="01. Candle Triggers")]
		public double TPPercent
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Stop In Profit", Order=250, GroupName="01. Candle Triggers")]
		public bool UseStopInProfit
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Stop In Profit Add Ticks",  Order=260, GroupName="01. Candle Triggers")]
		public int StopInProfitAddTicks
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Trade Every Candle", Order=300, GroupName="01. Candle Triggers")]
		public bool TradeEveryCandle
		{ get; set; }
		
	
		[NinjaScriptProperty]
		[Range(0.0001, double.MaxValue)]
		[Display(Name="Trailpercent", Order=1, GroupName="Parameters")]
		public double Trailpercent
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0.0001, double.MaxValue)]
		[Display(Name="Whick", Order=2, GroupName="Parameters")]
		public double Whick
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0.0001, double.MaxValue)]
		[Display(Name="TrendTrail", Order=3, GroupName="Parameters")]
		public double TrendTrail
		{ get; set; }

		[NinjaScriptProperty]
		[Range(-1000, int.MaxValue)]
		[Display(Name="Ltrail", Order=4, GroupName="Parameters")]
		public int Ltrail
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Strail", Order=5, GroupName="Parameters")]
		public int Strail
		{ get; set; }

		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="StartTime", Order=1, GroupName="Time")]
		public DateTime StartTime
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="EndTime", Order=2, GroupName="Time")]
		public DateTime EndTime
		{ get; set; }
		
		/*[NinjaScriptProperty]
		[Range(1.0, int.MaxValue)]
		[Display(Name="Contracts", Order=6, GroupName="Parameters")]
		public int Contracts
		{ get; set; }*/
		
		[NinjaScriptProperty]
		[Display(Name="Short On", Order=100, GroupName="Long Short")]
		public bool shortOn
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Long On", Order=200, GroupName="Long Short")]
		public bool longOn
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Trade Special Case", Order=300, GroupName="Long Short")]
		public bool tradeSpecialCase
		{ get; set; }
		
		
	
		
		#region RiskLogic
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="ProfitStop", Order=11, GroupName="Risk")]
		public double ProfitStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="LossStop", Order=12, GroupName="Risk")]
		public double LossStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="MaxTrades", Order=13, GroupName="Risk")]
		public double MaxTrades
		{ get; set; }
		#endregion
		
	
		#endregion

	}
}

