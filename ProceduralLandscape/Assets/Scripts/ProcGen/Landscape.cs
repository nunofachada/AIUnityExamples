/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * Author: Nuno Fachada
 * */
using System;

namespace LibGameAI.ProcGen
{
    public static class Landscape
    {

        public static void FaultModifier(
            float[,] landscape, float depth, Func<float> randFloat,
            float decreaseDistance = 0)
        {
            // Create random fault epicentre and direction vector
            float cx = randFloat.Invoke() * landscape.GetLength(0);
            float cy = randFloat.Invoke() * landscape.GetLength(1);
            float direction = randFloat.Invoke() * 2 * (float)Math.PI;
            float dx = (float)Math.Cos(direction);
            float dy = (float)Math.Sin(direction);

            // Apply the fault
            for (int x = 0; x < landscape.GetLength(0); x++)
            {
                for (int y = 0; y < landscape.GetLength(1); y++)
                {
                    // Get the dot product of the location with the fault
                    float ox = cx - x;
                    float oy = cy - y;
                    float dp = ox * dx + oy * dy;
                    float change;

                    // Positive dot product goes up, negative goes down
                    if (dp > 0)
                    {
                        // Fault size will decrease with distance if
                        // decreaseDistance > 0
                        float decrease = decreaseDistance > 0
                            ? decreaseDistance / (decreaseDistance + dp)
                            : 1;
                        // Positive dot product goes up
                        change = depth * decrease;
                    }
                    else
                    {
                        // Fault size will decrease with distance if
                        // decreaseDistance > 0
                        float decrease = decreaseDistance > 0
                            ? decreaseDistance / (decreaseDistance - dp)
                            : 1;
                        // Negative dot product goes down
                        change = -depth * decrease;
                    }

                    // Apply fault modification
                    landscape[x, y] += change;
                }
            }
        }

        // Per Bak sandpile model
        public static void Sandpile(float[,] landscape, float threshold,
            float increment, float decrement, float grainDropDensity,
            bool staticDrop, bool stochastic, Func<int, int> randInt,
            Func<double> randDouble,
            (int x, int y)[] neighs = null)
        {
            void Drop(int x, int y, float inc)
            {
                float inThresh = stochastic
                    ? (float)((NextNormalDouble(randDouble) +  threshold) * (increment / threshold))
                    : threshold;

                landscape[x, y] += inc;
                if (landscape[x, y] >= inThresh)
                {
                    float inDec = stochastic
                        ? (float)((NextNormalDouble(randDouble) +  decrement) * (increment / threshold))
                        : decrement;

                    landscape[x, y] -= inDec;
                    float slip = inDec / neighs.Length;
                    foreach ((int x, int y) neigh in neighs)
                    {
                        int nx = x + neigh.x;
                        int ny = y + neigh.y;
                        if (nx < 0 || nx >= landscape.GetLength(0) ||
                            ny < 0 || ny >= landscape.GetLength(1))
                        {
                            continue;
                        }
                        Drop(x + neigh.x, y + neigh.y, slip);
                    }
                }
            }

            int xDrop = randInt(landscape.GetLength(0));
            int yDrop = randInt(landscape.GetLength(1));
            int totalGrains = (int)(grainDropDensity * landscape.GetLength(0) * landscape.GetLength(1));
            if (neighs is null)
                neighs = new (int x, int y)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            for (int i = 0; i < totalGrains; i++)
            {
                Drop(xDrop, yDrop, increment);

                if (!staticDrop)
                {
                    xDrop = randInt(landscape.GetLength(0));
                    yDrop = randInt(landscape.GetLength(1));
                }
            }

        }

        // Public domain code from https://www.johndcook.com/blog/csharp_phi/
        private static double NormalCDF(double x, double mean = 0, double std = 1)
        {
            // constants
            const double a1 = 0.254829592;
            const double a2 = -0.284496736;
            const double a3 = 1.421413741;
            const double a4 = -1.453152027;
            const double a5 = 1.061405429;
            const double p = 0.3275911;

            // x after mean and std
            x = (x - mean) / std;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x) / Math.Sqrt(2.0);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return 0.5 * (1.0 + sign * y);
        }

        // Quick and dirty Box-Mueller transform, ignoring z1
        private static double NextNormalDouble(Func<double> nextUnifDouble)
        {

            double u1, u2;

            do
            {
                u1 = nextUnifDouble();
                u2 = nextUnifDouble();
            }
            while (u1 <= Double.Epsilon);

            double mag = Math.Sqrt(-2.0 * Math.Log(u1));
            double z0  = mag * Math.Cos(2 * Math.PI * u2);
            //double z1  = mag * Math.Sin(2 * Math.PI * u2);

            return z0;
        }


        // TODO Not working properly
        // Check https://github.com/creativitRy/Erosion
        // and "Fast Hydraulic Erosion Simulation and Visualization on GPU" by Xing Mei, Philippe Decaudin, Bao-Gang Hu
        // "Fast Hydraulic and Thermal Erosion on the GPU" by Balazs Jako
        public static void ThermalErosion(float[,] landscape, float threshold)
        {
            // This should be updated to use System.ReadOnlySpan when
            // Unity supports .NET Standard 2.1 in order to avoid heap
            // allocations
            (int, int)[] neighbors =
                new (int, int)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };

            // Create a copy of the landscape
            float[,] landscapeCopy =
                new float[landscape.GetLength(0), landscape.GetLength(1)];
            Array.Copy(landscape, landscapeCopy,
                landscape.GetLength(0) * landscape.GetLength(1));

            // Apply erosion
            for (int x = 1; x < landscape.GetLength(0) - 1; x++)
            {
                for (int y = 1; y < landscape.GetLength(1) - 1; y++)
                {
                    float height = landscapeCopy[x, y];
                    float limit = height - threshold;

                    foreach ((int x, int y) d in neighbors)
                    {
                        int nx = x + d.x;
                        int ny = y + d.y;
                        float nHeight = landscapeCopy[nx, ny];

                        // Is the neighbor below the threshold?
                        if (nHeight < limit)
                        {
                            // Some of the height moves, from 0 to 1/4 of the
                            // threshold, depending on the height difference
                            float delta = (limit - nHeight) / threshold;
                            if (delta > 2) delta = 2;
                            float change = delta * threshold / 8;

                            // Write new height to original landscape
                            landscape[x, y] = -change;
                            landscape[nx, ny] += change;
                        }
                    }
                }
            }
        }
    }
}
