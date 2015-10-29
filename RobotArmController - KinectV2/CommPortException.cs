namespace ThirdYearProject.RobotArmController.ArmInterface
{
    using System;

    public class CommPortException : ApplicationException
    {
        public CommPortException(Exception e)
            : base("Receive Thread Exception", e)
        {
        }

        public CommPortException(string desc)
            : base(desc)
        {
        }
    }
}
