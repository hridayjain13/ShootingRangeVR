using System;

namespace IVRAR.Group02.Telemetry
{
    /// <summary>
    /// Mathematical utilities for Signal Detection Theory (SDT) analysis.
    /// Calculates d' (sensitivity index), beta (likelihood ratio decision criterion),
    /// c (response bias), and A' (non-parametric sensitivity) using the Hautus (1995) log-linear correction.
    /// Reference: Stanislaw & Todorov (1999), Behavior Research Methods, Instruments, & Computers.
    /// </summary>
    public static class SignalDetectionMetrics
    {
        public struct SDTResult
        {
            public double HitRate;
            public double FalseAlarmRate;
            public double DPrime;
            public double Beta;
            public double ResponseBiasC;
            public double APrime;
            public int TotalSignals;
            public int TotalNoise;
        }

        /// <summary>
        /// Computes SDT metrics with Hautus log-linear transformation (adding 0.5 to each frequency cell).
        /// This eliminates infinities from 0% or 100% rates while preserving statistical properties.
        /// </summary>
        public static SDTResult Calculate(int hits, int misses, int falseAlarms, int correctRejections)
        {
            var res = new SDTResult
            {
                TotalSignals = hits + misses,
                TotalNoise = falseAlarms + correctRejections
            };

            if (res.TotalSignals == 0 || res.TotalNoise == 0)
            {
                res.DPrime = double.NaN;
                res.Beta = double.NaN;
                res.ResponseBiasC = double.NaN;
                res.APrime = double.NaN;
                return res;
            }

            // Hautus log-linear correction
            res.HitRate = (hits + 0.5) / (res.TotalSignals + 1.0);
            res.FalseAlarmRate = (falseAlarms + 0.5) / (res.TotalNoise + 1.0);

            double zH = InverseNormalCDF(res.HitRate);
            double zFA = InverseNormalCDF(res.FalseAlarmRate);

            res.DPrime = zH - zFA;
            res.ResponseBiasC = -0.5 * (zH + zFA);
            res.Beta = Math.Exp(-0.5 * (zH * zH - zFA * zFA));

            // Non-parametric sensitivity A' (Rae, 1976 / Stanislaw & Todorov, 1999)
            double h = res.HitRate;
            double fa = res.FalseAlarmRate;
            if (h >= fa)
            {
                res.APrime = 0.5 + ((h - fa) * (1.0 + h - fa)) / (4.0 * h * (1.0 - fa));
            }
            else
            {
                res.APrime = 0.5 - ((fa - h) * (1.0 + fa - h)) / (4.0 * fa * (1.0 - h));
            }

            return res;
        }

        /// <summary>
        /// Rational approximation for the inverse cumulative normal distribution (Beasley-Springer-Moro algorithm).
        /// Valid for p in (0, 1) with absolute error < 1.15e-9.
        /// </summary>
        public static double InverseNormalCDF(double p)
        {
            if (p <= 0.0) return -6.0;
            if (p >= 1.0) return 6.0;

            // Coefficients in rational approximations
            double[] a = {
                -3.969683028665376e+01,  2.209460984245205e+02,
                -2.759285104469687e+02,  1.383577518672690e+02,
                -3.066479806614716e+01,  2.506628277459239e+00
            };

            double[] b = {
                -5.447609879822406e+01,  1.615858368580409e+02,
                -1.556989798598866e+02,  6.680131188771972e+01,
                -1.328068155288572e+01
            };

            double[] c = {
                -7.784894002430293e-03, -3.223964580411365e-01,
                -2.400758277161838e+00, -2.549732539343734e+00,
                 4.374664141464968e+00,  2.938163982698783e+00
            };

            double[] d = {
                 7.784695709041462e-03,  3.224671290700398e-01,
                 2.445134137142996e+00,  3.754408661907416e+00
            };

            double q = p - 0.5;
            if (Math.Abs(q) <= 0.42)
            {
                double r = q * q;
                return q * (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) /
                           (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1.0);
            }

            double r2 = p < 0.5 ? p : 1.0 - p;
            double s = Math.Sqrt(-Math.Log(r2));
            double x = (((((c[0] * s + c[1]) * s + c[2]) * s + c[3]) * s + c[4]) * s + c[5]) /
                       ((((d[0] * s + d[1]) * s + d[2]) * s + d[3]) * s + 1.0);

            return p < 0.5 ? -x : x;
        }
    }
}
