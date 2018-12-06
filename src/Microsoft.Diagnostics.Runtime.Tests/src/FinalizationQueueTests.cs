using System.Linq;
using Shouldly;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class FinalizationQueueTests
    {
        static FinalizationQueueTests() =>Helpers.InitHelpers();

        [Fact]
        public void TestAllFinalizableObjects()
        {
            using (var dt = TestTargets.FinalizationQueue.LoadFullDump())
            {
                var runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                var targetObjectsCount = 0;
                
                foreach (var address in runtime.Heap.EnumerateFinalizableObjectAddresses())
                {
                    var type = runtime.Heap.GetObjectType(address);
                    if (type.Name == "DieFastA")
                        targetObjectsCount++;
                }
        
                targetObjectsCount.ShouldBe(42);
            }
        }
        
        [Fact]
        public void TestFinalizerQueueObjects()
        {
            using (var dt = TestTargets.FinalizationQueue.LoadFullDump())
            {
                var runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                var targetObjectsCount = 0;
                
                foreach (var address in runtime.EnumerateFinalizerQueueObjectAddresses())
                {
                    var type = runtime.Heap.GetObjectType(address);
                    if (type.Name == "DieFastB")
                        targetObjectsCount++;
                }
        
                targetObjectsCount.ShouldBe(13);
            }
        }
    }
}