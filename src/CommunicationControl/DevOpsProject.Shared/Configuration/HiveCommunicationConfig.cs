using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Configuration
{
    public class HiveCommunicationConfig
    {
        public bool IsHttpsConnected { get; set; } = false;
        public string RequestSchema { get; set; }
        public string CommunicationControlIP { get; set; }
        public int CommunicationControlPort { get; set; }
        public string CommunicationControlPath { get; set; }
        public string HiveIP { get; set; }
        public int HivePort { get; set; }
        public string HiveID { get; set; }
        public Location InitialLocation { get; set; }

        public override string ToString()
        {
            return $"HiveCommunicationConfig: IsHttpsConnected={IsHttpsConnected}, RequestSchema={RequestSchema}, CommunicationControlIP={CommunicationControlIP}, CommunicationControlPort={CommunicationControlPort}, CommunicationControlPath={CommunicationControlPath}, HiveIP={HiveIP}, HivePort={HivePort}, HiveID={HiveID}, InitialLocation={InitialLocation}";
        }
    }
}
