﻿namespace AtspmApi.Repositories
{
    public class DetectionHardwareRepositoryFactory
    {
        private static IDetectionHardwareRepository detectionHardwareRepository;

        public static IDetectionHardwareRepository Create()
        {
            if (detectionHardwareRepository != null)
                return detectionHardwareRepository;
            return new DetectionHardwareRepository();
        }

        public static void SetDetectionHardwareRepository(IDetectionHardwareRepository newRepository)
        {
            detectionHardwareRepository = newRepository;
        }
    }
}