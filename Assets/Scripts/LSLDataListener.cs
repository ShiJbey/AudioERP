namespace AudioERP
{
    public interface LSLDataListener
    {
        void PushDataSample(float[] dataSample, float sampleTime);
    }
}
