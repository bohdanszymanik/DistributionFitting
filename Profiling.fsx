#time

open System
open System.IO
open System.Globalization
open System.Text.RegularExpressions

let raw = File.ReadAllLines(@"C:\some performance data.2015-09-22.18.log")

// in this case the data looks a bit like this - repeating groups of timestamp and message lines
//Timestamp: 21/09/2015 11:29:05 p.m.
//Message: Request: <? xml.... and a whole bunch of stuff

let raw' = raw |> Array.filter (fun l -> l.StartsWith("Timestamp")) |> Array.map (fun s -> s.Substring(22,13))

// count arrivals per second
let cnts =
    raw' 
    |> Array.groupBy(fun s -> s)
    |> Array.map (fun (_,ts) -> (float)(Array.length ts))

File.WriteAllLines(@"c:\temp\out.txt", cnts |> Array.map (fun c -> (string)c))

// Accord framework is a great stats option for .net - http://www.codeproject.com/Articles/835786/Statistics-Workbench

#r @"packages\Accord\lib\net45\Accord.dll"
#r @"packages\Accord.Math\lib\net45\Accord.Math.dll"
#r @"packages\Accord.Statistics\lib\net45\Accord.Statistics.dll"

open Accord.Statistics
let h = Visualizations.Histogram(cnts)
// that's using the default constructor but you can adjust bin count etc by using 
// let h = Visualizations.Histogram()
// h.Compute(cnts, 30) for example
h.Bins |> Seq.map(fun b -> b.Value)
|> List.ofSeq

// plotting the histogram with fslab google chart works nicely too
#load "packages/FsLab/FsLab.fsx"

open Deedle
open FSharp.Data
open XPlot.GoogleCharts
open XPlot.GoogleCharts.Deedle
let histUpperEdges = h.Edges.[1..]
let histCnts = h.Values

let hists = Array.zip histUpperEdges histCnts
hists |> Chart.Column

// this is cool - but need to execute next two rows together to get fsi to print summary nicely
let n = new Distributions.Univariate.NormalDistribution()
n.Fit(cnts)

let p = new Distributions.Univariate.PoissonDistribution()
p.Fit(cnts)

// and get the goodness of fit - it automatically ranks popular distributions from greatest to least fit
// in this case, although it was counts of arrivals over time (ie should've been poisson) it ended up with normal, then gamma, then poisson
// which will be a result of caching and other activities occuring in the system under test

let da = new Analysis.DistributionAnalysis(cnts)
da.Compute()
da.GoodnessOfFit.[0]
// .Distribution to get mode details on a fitted distribution
da.GoodnessOfFit.[1]
da.GoodnessOfFit.[2]
da.GoodnessOfFit.[3]
