using Grpc;
using Grpc.Core;
using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Management;

namespace Grpc.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello" + request.Name
            });
        }

        public override async Task SayHelloStream(IAsyncStreamReader<HelloRequest> requestStream,
            IServerStreamWriter<HelloReply> replyStream, ServerCallContext context)
        {
            await foreach (var request in requestStream.ReadAllAsync())
            {
                await replyStream.WriteAsync(new HelloReply()
                {
                    Message = "Hello " + request.Name
                });
            }
        }



    }

    public class MetricsService : SystemMetrics.SystemMetricsBase
    {
        private readonly ILogger<MetricsService> _logger;
        public MetricsService(ILogger<MetricsService> logger)
        {
            _logger = logger;
        }

        public override async Task GetMetricsStream(IAsyncStreamReader<MetrixRequest> requestStream,
           IServerStreamWriter<MetricsResponse> replyStream, ServerCallContext context)
        {
            await foreach (var request in requestStream.ReadAllAsync())
            {
                float cpuUsage = GetCpuUsage();
                float availableMemory = GetAvailableMemory();
                float totalMemory = GetTotalMemory();
                (float freeDisk, float totalDisk) = GetDiskUsage();

                await replyStream.WriteAsync(new MetricsResponse()
                {
                    CpuUsage = cpuUsage,
                    AvailableMemoryMb = availableMemory,
                    TotalMemoryMb = totalMemory,
                    FreeDiskSpaceGb = freeDisk,
                    TotalDiskSpaceGb = totalDisk
                });
            }
        }

        private float GetCpuUsage()
        {
            var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
            return searcher.Get().Cast<ManagementObject>().Select(m => Convert.ToSingle(m["LoadPercentage"])).FirstOrDefault();
        }

        private float GetAvailableMemory()
        {
            var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
            return searcher.Get().Cast<ManagementObject>().Select(m => Convert.ToSingle(m["FreePhysicalMemory"]) / 1024).FirstOrDefault();
        }

        private float GetTotalMemory()
        {
            var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
            return searcher.Get().Cast<ManagementObject>().Select(m => Convert.ToSingle(m["TotalVisibleMemorySize"]) / 1024).FirstOrDefault();
        }

        private (float freeDisk, float totalDisk) GetDiskUsage()
        {
            DriveInfo drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
            if (drive != null)
            {
                return (drive.TotalFreeSpace / (1024f * 1024 * 1024), drive.TotalSize / (1024f * 1024 * 1024));
            }
            return (0, 0);
        }
    }
}
