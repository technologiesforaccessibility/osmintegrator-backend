using System.Threading.Tasks;
using OsmIntegrator.Interfaces;

namespace OsmIntegrator.Interfaces {
    public interface IUseCase<in TInput> {
        public Task<AUseCaseResponse> Handle(TInput useCaseData);
    }
}
