using System.Threading.Tasks;
using OsmIntegrator.Interfaces;

namespace OsmIntegrator.Interfaces {
    public interface IUseCase<in TInput> {
        Task<AUseCaseResponse> Handle(TInput useCaseData);
    }
}
