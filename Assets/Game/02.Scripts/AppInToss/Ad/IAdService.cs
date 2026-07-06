using Cysharp.Threading.Tasks;
using JumJump.Data;
using System.Threading;

namespace JumJump.Interface
{
    public interface IAdService
    {
        UniTask<AdResult> ShowRewardedAdAsync(string adGroupId, CancellationToken cancellationToken = default);
    }
}
