using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Data;

namespace JumJump.Interface
{
    public interface IIAPService
    {
        UniTask<IAPResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default);
    }
}
