import FactoryKit

extension Container {
    @MainActor
    var homeService: Factory<any HomeProviding> {
        self { @MainActor in HomeMock() }
    }

    @MainActor
    var homeViewModel: Factory<HomeViewModel> {
        self { @MainActor in HomeViewModel(service: self.homeService()) }
    }
}
