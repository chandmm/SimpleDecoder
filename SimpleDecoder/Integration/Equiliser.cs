// Adapted from:
// /***************************************************************************
//  * Equalizer.cs
//  * Copyright (c) 2015 the authors.
//  * 
//  * All rights reserved. This program and the accompanying materials
//  * are made available under the terms of the GNU Lesser General Public License
//  * (LGPL) version 3 which accompanies this distribution, and is available at
//  * https://www.gnu.org/licenses/lgpl-3.0.en.html
//  *
//  * This library is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  * Lesser General Public License for more details.
//  *
//  ***************************************************************************/

namespace SimpleMp3Decoder.Intergration
{
    /// <summary>
    ///     The Equalizer class can be used to specify
    ///     equalization settings for the MPEG audio decoder.
    ///     The equalizer consists of 32 band-pass filters.
    ///     Each band of the equalizer can take on a fractional value between
    ///     -1.0 and +1.0.
    ///     At -1.0, the input signal is attenuated by 6dB, at +1.0 the signal is
    ///     amplified by 6dB.
    /// </summary>
    public class Equaliser
    {
        private const int BANDS = 32;

        /// <summary>
        ///     Equalizer setting to denote that a given band will not be
        ///     present in the output signal.
        /// </summary>
        public const double BAND_NOT_PRESENT = double.NegativeInfinity;

        public static readonly Equaliser PASS_THRU_EQ = new Equaliser();
        private double[] settings;

        /// <summary>
        ///     Creates a new Equalizer instance.
        /// </summary>
        public Equaliser()
        {
            InitBlock();
        }

        //	private Equalizer(double b1, double b2, double b3, double b4, double b5,
        //					 double b6, double b7, double b8, double b9, double b10, double b11,
        //					 double b12, double b13, double b14, double b15, double b16,
        //					 double b17, double b18, double b19, double b20);

        public Equaliser(double[] settings)
        {
            InitBlock();
            FromdoubleArray = settings;
        }

        public Equaliser(EQFunction eq)
        {
            InitBlock();
            FromEQFunction = eq;
        }

        public double[] FromdoubleArray
        {
            set
            {
                reset();
                int max = (value.Length > BANDS) ? BANDS : value.Length;

                for (int i = 0; i < max; i++)
                {
                    settings[i] = limit(value[i]);
                }
            }
        }

        //UPGRADE_TODO: Method 'setFrom' was converted to a set modifier. This name conflicts with another property. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1137"'
        /// <summary>
        ///     Sets the bands of this equalizer to the value the bands of
        ///     another equalizer. Bands that are not present in both equalizers are ignored.
        /// </summary>
        public virtual Equaliser FromEqualizer
        {
            set
            {
                if (value != this)
                {
                    FromdoubleArray = value.settings;
                }
            }
        }

        //UPGRADE_TODO: Method 'setFrom' was converted to a set modifier. This name conflicts with another property. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1137"'
        public EQFunction FromEQFunction
        {
            set
            {
                reset();
                int max = BANDS;

                for (int i = 0; i < max; i++)
                {
                    settings[i] = limit(value.getBand(i));
                }
            }
        }

        /// <summary>
        ///     Retrieves the number of bands present in this equalizer.
        /// </summary>
        public virtual int BandCount => settings.Length;

        /// <summary>
        ///     Retrieves an array of doubles whose values represent a
        ///     scaling factor that can be applied to linear samples
        ///     in each band to provide the equalization represented by
        ///     this instance.
        /// </summary>
        /// <returns>
        ///     an array of factors that can be applied to the
        ///     subbands.
        /// </returns>
        internal virtual double[] BandFactors
        {
            get
            {
                double[] factors = new double[BANDS];
                for (int i = 0, maxCount = BANDS; i < maxCount; i++)
                {
                    factors[i] = GetBandFactor(settings[i]);
                }

                return factors;
            }
        }

        private void InitBlock()
        {
            settings = new double[BANDS];
        }

        /// <summary>
        ///     Sets all bands to 0.0
        /// </summary>
        public void reset()
        {
            for (int i = 0; i < BANDS; i++)
            {
                settings[i] = 0.0f;
            }
        }

        public double SetBand(int band, double neweq)
        {
            double eq = 0.0f;

            if ((band >= 0) && (band < BANDS))
            {
                eq = settings[band];
                settings[band] = limit(neweq);
            }

            return eq;
        }

        /// <summary>
        ///     Retrieves the eq setting for a given band.
        /// </summary>
        public double getBand(int band)
        {
            double eq = 0.0f;

            if ((band >= 0) && (band < BANDS))
            {
                eq = settings[band];
            }

            return eq;
        }

        private double limit(double eq)
        {
            if (eq == BAND_NOT_PRESENT)
                return eq;
            if (eq > 1.0f)
                return 1.0f;
            if (eq < -1.0f)
                return -1.0f;

            return eq;
        }

        /// <summary>
        ///     Converts an equalizer band setting to a sample factor.
        ///     The factor is determined by the function f = 2^n where
        ///     n is the equalizer band setting in the range [-1.0,1.0].
        /// </summary>
        internal double GetBandFactor(double eq)
        {
            if (eq == BAND_NOT_PRESENT)
                return 0.0f;

            double f = (double)Math.Pow(2.0, eq);
            return f;
        }

        public abstract class EQFunction
        {
            /// <summary>
            ///     Returns the setting of a band in the equalizer.
            /// </summary>
            /// <param name="band	The">
            ///     index of the band to retrieve the setting
            ///     for.
            /// </param>
            /// <returns>
            ///     the setting of the specified band. This is a value between
            ///     -1 and +1.
            /// </returns>
            public virtual double getBand(int band)
            {
                return 0.0f;
            }
        }
    }
}
