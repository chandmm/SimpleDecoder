//Mostly code derived from Mp3Sharp
namespace SimpleMp3Decoder.Intergration
{
    public class Params : ICloneable
    {
        private Equaliser m_Equalizer;

        /// <summary>
        ///     Retrieves the equalizer settings that the decoder's equalizer
        ///     will be initialized from.
        ///     The Equalizer instance returned
        ///     cannot be changed in real time to affect the
        ///     decoder output as it is used only to initialize the decoders
        ///     EQ settings. To affect the decoder's output in realtime,
        ///     use the Equalizer returned from the getEqualizer() method on
        ///     the decoder.
        /// </summary>
        /// <returns>
        ///     The Equalizer used to initialize the
        ///     EQ settings of the decoder.
        /// </returns>
        public virtual Equaliser InitialEqualizerSettings => m_Equalizer;

        public object Clone()
        {
            try
            {
                return MemberwiseClone();
            }
            catch (Exception ex)
            {
                throw new ApplicationException(this + ": " + ex);
            }
        }
    }
}
