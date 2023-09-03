using System.Text;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WalletService.Dtos;
using WalletService.RequestModels;

namespace WalletService.IntegrationTest.Steps;

[Binding]
public class WalletManagementSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly HttpClient _httpClient;
    private const string CreateWalletResponse = "CreateWalletResponse";
    private const string CreateWalletRequest = "CreateWalletRequest";

    public WalletManagementSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _httpClient = _scenarioContext.Get<HttpClient>("HttpClient");
    }

    [Given(@"a running Wallet API")]
    public async Task GivenARunningWalletApi()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("/health");
        response.EnsureSuccessStatusCode().IsSuccessStatusCode.Should().BeTrue();
    }

    [Given(@"I have a valid wallet creation request")]
    public void GivenIHaveAValidWalletCreationRequest()
    {
        var createWalletRequest = GetCreateWalletRequest();
        _scenarioContext.Set(createWalletRequest, CreateWalletRequest);
    }

    [When(@"I send the create wallet request")]
    public async Task WhenISendTheCreateWalletRequest()
    {
        var request = _scenarioContext.Get<CreateWalletRequest>(CreateWalletRequest);

        var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
            "application/json");

        var responseMessage = await _httpClient.PostAsync("/Wallet", content);

        _scenarioContext.Set(responseMessage, CreateWalletResponse);
    }

    [Then(@"the wallet should be created successfully")]
    public async Task ThenTheWalletShouldBeCreatedSuccessfully()
    {
        var request = _scenarioContext.Get<CreateWalletRequest>(CreateWalletRequest);
        var response = _scenarioContext.Get<HttpResponseMessage>(CreateWalletResponse);
        response.EnsureSuccessStatusCode();

        var wholeResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
        var data = wholeResponse["data"].ToString()!;

        var result = JsonConvert.DeserializeObject<WalletDto>(data);

        // Assertion
        result.Should().NotBeNull();
        result.walletId.Should().NotBe(Guid.Empty);
        result.UserId.Should().Be(request.UserId);
    }

    [Given(@"I have an existing wallet")]
    public async Task GivenIHaveAnExistingWallet()
    {
        var createWalletRequest = GetCreateWalletRequest();

        var content = new StringContent(JsonConvert.SerializeObject(createWalletRequest), Encoding.UTF8,
            "application/json");

        var responseMessage = await _httpClient.PostAsync("/Wallet", content);

        var wholeResponse = JObject.Parse(await responseMessage.Content.ReadAsStringAsync());
        var data = wholeResponse["data"].ToString()!;

        var result = JsonConvert.DeserializeObject<WalletDto>(data);
        _scenarioContext.Set(result, "CreatedWallet");
    }

    [Given(@"I have a valid wallet update request")]
    public void GivenIHaveAValidWalletUpdateRequest()
    {
        var createdWallet = _scenarioContext.Get<WalletDto>("CreatedWallet");

        var updateRequest = new UpdateBalanceRequest
        {
            CorrelationId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            Amount = 15,
            WalletId = createdWallet.walletId
        };
        _scenarioContext["UpdateRequest"] = updateRequest;
    }


    [When(@"I send the update wallet request")]
    public async Task WhenISendTheUpdateWalletRequest()
    {
        var updateRequest = (UpdateBalanceRequest)_scenarioContext["UpdateRequest"];
        var content = new StringContent(JsonConvert.SerializeObject(updateRequest), Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync("/wallet", content);

        _scenarioContext["UpdateResponse"] = response;
    }

    [When(@"I send a get wallet request")]
    public async Task WhenISendAGetWalletRequest()
    {
        var createdWallet = _scenarioContext.Get<WalletDto>("CreatedWallet");
        var response = await _httpClient.GetAsync($"/wallet/{createdWallet.walletId}");
        _scenarioContext["GetResponse"] = response;
    }

    [Then(@"the wallet should be updated successfully")]
    public void ThenTheWalletShouldBeUpdatedSuccessfully()
    {
        var response = (HttpResponseMessage)_scenarioContext["UpdateResponse"];
        response.EnsureSuccessStatusCode();
    }

    [Then(@"the wallet details should be retrieved successfully")]
    public void ThenTheWalletDetailsShouldBeRetrievedSuccessfully()
    {
        var response = (HttpResponseMessage)_scenarioContext["GetResponse"];
        response.EnsureSuccessStatusCode();
    }

    private CreateWalletRequest GetCreateWalletRequest()
    {
        return new CreateWalletRequest
        {
            UserId = Guid.NewGuid(),
            FullName = "Test User",
            Balance = 0,
            CorrelationId = Guid.NewGuid()
        };
    }
}