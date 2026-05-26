using BetterIdentityV.Core.Config;

namespace BetterIdentityV.Service.Interface;

public interface IConfigService
{
    AllConfig Get();

    void Save();

    AllConfig Read();

    void Write(AllConfig config);
}