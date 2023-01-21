  (function (w) {
  w.URLSearchParams = w.URLSearchParams || function (searchString) {
    var self = this;
    self.searchString = searchString;
    self.get = function (name) {
      const results = new RegExp('[\?&]' + name + '=([^&#]*)').exec(self.searchString)
      if (results == null) {
        return null;
      }
      else {
        return decodeURI(results[1]).toLowerCase() || 0;
      }
    };
  }
})(window)

  function loadStylesheet(url) {
  if (document.createStyleSheet) {
  document.createStyleSheet(url);
} else {
  var styles = "@import url(" + url + ");";
  var newSS = document.createElement('link');
  newSS.rel = 'stylesheet';
  newSS.href = 'data:text/css,' + escape(styles);
  document.getElementsByTagName("head")[0].appendChild(newSS);
}
}

function setFavicon(url) {
  var link = document.querySelector("link[rel~='icon']");
  if (!link) {
  link = document.createElement('link');
  link.rel = 'icon';
  document.getElementsByTagName('head')[0].appendChild(link);
}
  link.href = url;
}

  web3Account = "";
  signature = "";
  defaultDeviceId = "";
  vueMounted = false
  windowLoaded = false
  outstandingSignatureRequest = false;
  const Web3Modal = window.Web3Modal.default;
  const WalletConnectProvider = window.WalletConnectProvider.default;
  ETHEREUM = "ETHEREUM";
  TEZOS = "TEZOS";

  /**
  * Setup the Web3Modal
  */
function web3ModalInit() {

  console.log("Initializing example");
  console.log("WalletConnectProvider is", WalletConnectProvider);
  console.log("Fortmatic is", Fortmatic);
  console.log("window.web3 is", window.web3, "window.ethereum is", window.ethereum);

  // Check that the web page is run in a secure context,
  // as otherwise MetaMask won't be available
  if (location.protocol !== 'https:') {
  // https://ethereum.stackexchange.com/a/62217/620
  return;
}

  // Tell Web3modal what providers we have available.
  // Built-in web browser provider (only one can exist as a time)
  // like MetaMask, Brave or Opera is added automatically by Web3modal
const providerOptions = {
  authereum: {
  package: Authereum // required
},
  walletconnect: {
  package: WalletConnectProvider,
  options: {
  infuraId: "9d5e849c49914b7092581cc71e3c2580",
}
},
  // Bug: https://github.com/Web3Modal/web3modal/issues/231
  //fortmatic: {
  //    package: Fortmatic,
  //    options: {
  //        key: "pk_test_BCDB8DBBFE1F5B55"
  //    }
  //},
  //bitski: {
  //    package: Bitski, // required
  //    options: {
  //        clientId: "76674289-906f-451a-9aa4-6353f3bc442a", // required
  //        callbackUrl: "https://tokencast.net/account" // required
  //    }
  //}
};

  web3Modal = new Web3Modal({
  cacheProvider: true, // optional
  providerOptions, // required
  disableInjectedProvider: false, // optional. For MetaMask / Brave / Opera.
});

  console.log("Web3Modal instance is", web3Modal);
}

async function initProvider() {
  try {
  provider = await web3Modal.connect();
} catch (e) {
  console.log("Could not get a wallet connection", e);
  return;
}

  // Subscribe to accounts change
provider.on("accountsChanged", (accounts) => {
  LoginUser();
});

  // Subscribe to chainId change
provider.on("chainChanged", (chainId) => {
  LoginUser();
});

  // Subscribe to networkId change
provider.on("networkChanged", (networkId) => {
  LoginUser();
});
}

async function getTezosAccount() {
  await initTezosAccount(true);
}

async function onConnectEthereum() {
  app.network = ETHEREUM;
  await initProvider();
  await LoginUser();
}

async function onConnectTezos() {
  app.network = TEZOS;
  await getTezosAccount();
  await LoginUser();
}

async function onLoad(mounted, loaded) {
  vueMounted = vueMounted || mounted;
  windowLoaded = windowLoaded || loaded;
  if (vueMounted && windowLoaded) {
    //Web3Modal adds the key "WEB3_CONNECT_CACHED_PROVIDER" in localStorage after a successful connection, value is the provider name
    web3ModalCachedProvider = window.localStorage.getItem("WEB3_CONNECT_CACHED_PROVIDER")
    if (web3ModalCachedProvider !== undefined && web3ModalCachedProvider !== "" && web3ModalCachedProvider !== null) {
      web3ModalInit();
      await initProvider();
      await initEthereumAccount();
      await GetAccountIfSignatureExists(ETHEREUM);
    } else {
      await initTezosAccount(false)
      await GetAccountIfSignatureExists(TEZOS);
    }
  }
}

function checkWhitelabel() {
  for (var whiteLabeler in whiteLabelers) {
  if (window.location.origin.indexOf(whiteLabelers[whiteLabeler].url) >= 0) {
    app.whitelabeler = whiteLabeler;
    break;
  }
}
if (!app.whitelabeled) {
  var urlParams = new URLSearchParams(window.location.search);
  whitelabeler = urlParams.get("whitelabel");
  app.whitelabeler = whitelabeler;
}
  if (app.whitelabeled) {
    document.title = whiteLabelers[app.whitelabeler].title;
    if (whiteLabelers[app.whitelabeler].css !== undefined) {
      loadStylesheet(window.location.origin + "/css/" + whiteLabelers[app.whitelabeler].css, document)
    }
    if (whiteLabelers[app.whitelabeler].favicon !== undefined) {
      setFavicon(window.location.origin + "/images/" + whiteLabelers[app.whitelabeler].favicon)
    }
  }

}

window.addEventListener('load', async () => {
  checkWhitelabel();
  web3ModalInit()
  getLastUsedDevice();
}, false);

async function initEthereumAccount() {
  app.providedWeb3 = new Web3(provider);
  web3Account = (await app.providedWeb3.eth.getAccounts())[0];
  app.address = web3Account;
  await AttemptReverse(web3Account);
}

async function initTezosAccount(onConnect) {
  try {
    if (!app.dAppClient) {
      app.dAppClient = await new beacon.DAppClient({ name: "TokenCast" });
    }
    activeAccount = await app.dAppClient.getActiveAccount();

    if (!activeAccount && onConnect) {
      console.log("getting permissions");
      const permissions = await app.dAppClient.requestPermissions();
      activeAccount = await app.dAppClient.getActiveAccount();
    }
    if (activeAccount) {
      app.address = activeAccount.address;
      web3Account = app.address;
    }
  } catch (error) {
    console.log("Tezos Account error:", error);
  }
}

async function LoginUser() {
  if (app.network === TEZOS) {
    app.showSignMessage = true;
    await GetSignature();
  } else {
    try {
      await initEthereumAccount();
      await GetSignature();
    }
    catch {
      app.showSignMessage = true;
      web3ModalInit();
    }
  }
}

async function GetAccountIfSignatureExists(network) {
  signature = getCachedSignature(web3Account, network);
  if (signature !== undefined && signature !== "" && signature !== null) {
    if (network === TEZOS) {
      app.tezosWalletConnected = true;
    }
    app.showSignMessage = false;
    app.network = network;
    await app.FetchCanviaDevices();
    await GetAccountInfo(true);
  }
}

async function AttemptReverse(address) {
  let provider = new ethers.providers.Web3Provider(app.providedWeb3.currentProvider);
  provider.lookupAddress(address).then(function (ensName) {
    if (ensName != null) {
      app.address = ensName;
    }
  });
}

function getSignatureKey(address, walletNetwork) {
  return address + '_signature' + '_' + walletNetwork
}

function getCachedSignature(address, walletNetwork) {
  var key = getSignatureKey(address, walletNetwork);
  var cachedSignature = window.localStorage.getItem(key);
  return cachedSignature
}

function cacheSignature(signature, address, walletNetwork) {
  var key = getSignatureKey(address, walletNetwork);
  window.localStorage.setItem(key, signature);
}

function storeLastUsedDevice(deviceId) {
  app.selected = deviceId;
  window.localStorage.setItem("lastDevice", deviceId);
}

function getLastUsedDevice() {
  var deviceId = window.localStorage.getItem("lastDevice");
  app.selected = deviceId;
  return deviceId;
}

async function GetSignature() {
  signature = getCachedSignature(web3Account, app.network);
  if (signature === undefined || signature === "" || signature === null) {
    if (!outstandingSignatureRequest) {
      outstandingSignatureRequest = true;
      try {
        if (app.network === TEZOS) {
          try {
            console.log("Getting Tezos signature");
            //const response = await app.dAppClient.requestSignPayload({
            //    signingType: beacon.SigningType.RAW,
            //    payload: app.signatureMessage,
            //});
            app.tezosWalletConnected = true;
            //signature = response.signature;
          } catch (e) {
            console.log(e);
          }
        } else {
          console.log("Getting Ethereum signature");
          let plain = app.signatureMessage;
          let msg = app.providedWeb3.utils.asciiToHex(plain);
          signature = await app.providedWeb3.eth.personal.sign(msg, web3Account);
        }
        cacheSignature(signature, web3Account, app.network)
      } finally {
        outstandingSignatureRequest = false;
      }
    }
  }

  if (signature !== undefined && signature !== "" && signature !== null) {
    await app.FetchCanviaDevices();
  }
    await GetAccountInfo(true);
  }

const origURL = 'https://nftframe.azurewebsites.net/'

async function GetAccountInfo(onPageLoad) {
  // Create account if not exist
  $.get(origURL + "Account/Details?address=" + web3Account + "&signature=" + signature + "&network=" + app.network + "&whitelabeler=" + app.whitelabeler, function (accountDetails) {
    if (accountDetails != null) {
      app.showSignMessage = false;
      app.account = accountDetails;
      app.showAddDeviceButton = true;
      if (onPageLoad) {
        CheckAddDevice(accountDetails);
      }
    } else {
      console.log("Error fetching account details");
    }
  });
}

async function CheckAddDevice(accountDetails) {
  // Get device id from QS param
  // If present, prompt user to add device
  var urlParams = new URLSearchParams(window.location.search);
  var deviceId = urlParams.get("deviceId");
  var deviceAlias = deviceId

  if (deviceId != null && deviceAlias != null &&
    (accountDetails.devices == null || accountDetails.devices.indexOf(deviceId) == -1)) {
    AddDevice(deviceId, deviceAlias);
  }
  else if (accountDetails.devices != null && accountDetails.devices.length > 0) {
    defaultDeviceId = accountDetails.devices[0];
    GetTokens();
  }
  else {
    // No devices found
    GetTokens();
    app.showAddDeviceInput = true;
  }
}

async function AddDevice(deviceId, deviceAlias) {
  $.post(origURL + "Account/AddDevice?address=" + web3Account + "&signature=" + signature + "&network=" + app.network + "&deviceId=" + deviceId + "&deviceAlias=" + deviceAlias + "&whitelabeler=" + app.whitelabeler,
    function (result) {
      if (result == false) {
        console.log("Failed to link device to account");
      }
      else {
        console.log("Successfully linked device to account");
        defaultDeviceId = deviceId;
        GetAccountInfo();
        GetTokens();
      }
    });
}

async function AddCanviaDevices(code, address) {
  $.post(origURL + "Account/AddCanviaDevices?address=" + address + "&signature=" + signature + "&network=" + app.network + "&code=" + code + "&whitelabeler=" + app.whitelabeler,
    function (result) {
      if (result === false) {
        console.log("Failed to link Canvia account");
      }
      else {
        console.log("Successfully linked Canvia account. Devices Added!");
        app.showAddDeviceInput = false;
        GetAccountInfo(true);
      }
    });
}

async function UpdateDevice(deviceId, alias, frequency) {
  $.post(origURL + "Account/UpdateDevice?address=" + web3Account + "&signature=" + signature + "&network=" + app.network + "&deviceId=" + deviceId + "&alias=" + alias + "&frequency=" + frequency + "&whitelabeler=" + app.whitelabeler,
    function (result) {
      if (result === false) {
        console.log("Failed to add device alias");
      }
      else {
        console.log("Successfully added device alias");
        GetAccountInfo();
      }
    });
}

async function GetDeviceRotationFrequency(deviceId) {
  $.post(origURL + "Account/GetDeviceFrequency?address=" + web3Account + "&signature=" + signature + "&network=" + app.network + "&deviceId=" + deviceId + "&whitelabeler=" + app.whitelabeler,
    function (result) {
      if (result == null) {
        console.log("Failed to fetch device rotation frequency");
      }
      else {
        app.rotationFrequency = parseInt(result);
      }
    });
}

async function DeleteDevice(deviceId) {
  $.post(origURL + "Account/DeleteDevice?address=" + web3Account + "&signature=" + signature + "&network=" + app.network + "&deviceId=" + deviceId + "&whitelabeler=" + app.whitelabeler,
    function (result) {
      if (!result) {
        console.log("Failed to delete device");
      }
      else {
        console.log("Successfully deleted device");
        GetAccountInfo();
      }
    });
}

async function RemoveAllTokens(deviceId) {
  $.post(origURL + "Account/RemoveDeviceContent?address=" + web3Account + "&signature=" + signature + "&network=" + app.network + "&deviceId=" + deviceId + "&whitelabeler=" + app.whitelabeler,
    function (result) {
      if (result === false) {
        alert("Remove Failed");
      }
      else {
        alert("Removed Token!");
      }
    });
}

async function RemoveSingleToken(deviceId, index) {
  $.post(origURL + "Account/RemoveIndexFromQueue?address=" + web3Account + "&signature=" + signature + "&network=" + app.network + "&deviceId=" + deviceId + "&index=" + index + "&whitelabeler=" + app.whitelabeler,
    function (result) {
      if (result === false) {
        alert("Remove Failed");
      }
      else {
        alert(`Removed Token number ${index + 1}!`);
        app.tokensByDevice = JSON.parse(result);
      }
    });
}

async function GetCastedTokensForDevice(deviceId) {
  $.post("Account/GetCastedTokensForDevice?address=" + web3Account + "&signature=" + signature + "&network=" + app.network + "&deviceId=" + deviceId + "&whitelabeler=" + app.whitelabeler,
    function (result) {
      if (result === false) {
        console.log("Could not get tokens for device");
      }
      else {
        const parsedTokens = JSON.parse(result);
        app.tokensByDevice = parsedTokens;
        console.log("Fetched tokens on queue! " + parsedTokens)
      }
    });
}

async function GetTokens() {
  app.showFetchingTokensMessage = true;
  $.get(origURL + "Account/Tokens?address=" + web3Account + "&signature=" + signature + "&network=" + app.network + "&whitelabeler=" + app.whitelabeler, function (tokenResponse) {
    app.showNoTokensMessage = tokenResponse === "";
    var parsedTokens = JSON.parse(tokenResponse);
    if (parsedTokens == null || parsedTokens.assets.length === 0) {
      // no tokens found
      app.showNoTokensMessage = true;
    }
    app.showFetchingTokensMessage = false;
    app.tokens = [];
    parsedTokens.assets.forEach(function (token) {
      if (token.image_url !== "") {
        app.tokens.push(token);
      }
    })
    app.tokensLoaded = true;
  });

  await GetCommunityTokens()
}

  async function GetCommunityTokens() {

  app.showFetchingTokensMessage = true;

  $.get(origURL + "Account/CommunityTokens", function (tokenResponse) {
  var parsedTokens = JSON.parse(tokenResponse);
  app.showFetchingTokensMessage = false;
  app.communityTokens = [];
  parsedTokens.assets.forEach(function (token) {
  if (token.image_url !== "") {
  app.communityTokens.push(token);
}
})
});
}

  function CopyToClipboard(str) {
  const el = document.createElement('textarea');
  el.value = str;
  el.setAttribute('readonly', '');
  el.style.position = 'absolute';
  el.style.left = '-9999px';
  document.body.appendChild(el);
  el.select();
  document.execCommand('copy');
  document.body.removeChild(el);
};

  function grabColorFromPos(e) {
  if (e.offsetX) {
  x = e.offsetX;
  y = e.offsetY;
}
  else if (e.layerX) {
  x = e.layerX;
  y = e.layerY;
}

  var tokenPreviewContainer = document.getElementById('tokenPreviewContainer');
  var tokenPreviewCanvas = document.getElementById('tokenPreviewCanvas');

  tokenPreviewCanvas.width = tokenPreviewContainer.width;
  tokenPreviewCanvas.height = tokenPreviewContainer.height;
  fitImageOn(tokenPreviewCanvas, tokenPreviewContainer, tokenPreviewContainer.style.objectFit == "contain");
  var p = tokenPreviewCanvas.getContext('2d')
  .getImageData(x, y, 1, 1).data;

  var hexColor = rgbToHex(p[0], p[1], p[2]);
  return hexColor;
}

  function fitImageOn(canvas, imageObj, contain) {
  var context = canvas.getContext('2d');
  var imageAspectRatio = imageObj.naturalWidth / imageObj.naturalHeight;
  var canvasAspectRatio = canvas.width / canvas.height;
  var renderableHeight, renderableWidth, xStart, yStart;

  // Simulate 'cover' if not contain
  if (!contain) {
  // Width = canvas width
  // Height = ratio
  renderableWidth = canvas.width;
  renderableHeight = canvas.width / imageAspectRatio;
  xStart = 0;
  yStart = (canvas.height - renderableHeight) / 2;
}
  // If image's aspect ratio is less than canvas's we fit on height
  // and place the image centrally along width
  else if (imageAspectRatio < canvasAspectRatio) {
  renderableHeight = canvas.height;
  renderableWidth = imageObj.naturalWidth * (renderableHeight / imageObj.naturalHeight);
  xStart = (canvas.width - renderableWidth) / 2;
  yStart = 0;
}

  // If image's aspect ratio is greater than canvas's we fit on width
  // and place the image centrally along height
  else if (imageAspectRatio > canvasAspectRatio) {
  renderableWidth = canvas.width
  renderableHeight = imageObj.naturalHeight * (renderableWidth / imageObj.naturalWidth);
  xStart = 0;
  yStart = (canvas.height - renderableHeight) / 2;
}

  // Happy path - keep aspect ratio
  else {
  renderableHeight = canvas.height;
  renderableWidth = canvas.width;
  xStart = 0;
  yStart = 0;
}

  context.drawImage(imageObj, xStart, yStart, renderableWidth, renderableHeight);
};

  function rgbToHex(r, g, b) {
  return "#" + componentToHex(r) + componentToHex(g) + componentToHex(b);
}

  function componentToHex(c) {
  var hex = c.toString(16);
  return hex.length == 1 ? "0" + hex : hex;
}
