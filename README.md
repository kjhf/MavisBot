# MavisBot

### Summary
Mavis is a Splatoon-themed general purpose C# Discord bot maintained by Slate. Rewritten / migrated from three previous Discord bots. Built with [Discord.NET](https://github.com/discord-net/Discord.Net).

### Help
  - [Development server](https://discord.gg/Px5Bhny)

If you'd like to help, come talk to me on Discord or submit a pull request or issue report.

### Why another bot?
The two previous C# bots suffered from a lot of needless code to make commands configurable, when it's simply not needed. 

The third bot is my successful Python bot, Dola, however since discord.py is no longer in service, requires a full rewrite to use another framework. Further, to use SplatTag (Slapp), serialization between Slapp and Dola is required. However, Mavis can natively depend on Slapp. 

### Why Mavis?
A reference to a hilarious topic of conversation Wug and I had. Also, Mavis is a thrush/songbird, and we love our birbs. 

### Licensing
Mavis is provided as-is and this source code is meant as a reference. You may pick out bits for your own project, just don't pass off the whole project as your own, kay?

### TODOs
Please see the [issues tab](https://github.com/kjhf/MavisBot/issues) and suggestions in the development server.


### Setup for something.host
* https://cp.something.host/dashboard
* Transfer files using creds on remote connection, use sftp://213.xxx.xxx.xxx as the hostname on FileZilla.
* The container is a Linux build with $HOME set to /home/container/  -- note this for any assumptions about ~ !


### Old Azure Dockerised setup (not required)
* The Dockerfile assumes SplatTag is under /publish. Adjust if necessary.
  * First, grab SplatTag and put it into the Docker build context, e.g.
  * `. GrabSplatTag.bat`
  
THEN
* With Docker Desktop running,
* `docker build --no-cache --pull --tag="slate.azurecr.io/mavis:latest" -f Dockerfile .`

### Test or run with 
* `docker run -t -d slate.azurecr.io/mavis`
* Recommended tests:
  * Expected result, e.g. "slate"
  * Query that has players and teams, e.g. "squid" - should be green
  * Multiple teams query e.g. "squid --team" - should be gold
  * Multiple players query "squid --player" - should be blue
  * Test reacts (1 and 20)
  * Single player result, e.g. react to the above - should be dark gold
  * Single team, e.g. react to the above - should be dark blue
  * Test a plus member, e.g. Sendou


### Update Azure Image with
After the build step, (note these commands are long in this window!)
* `az login`
* `az acr login --name slate`
* `docker push slate.azurecr.io/mavis:latest`
* To stop:
  * `az container stop --name mavis --resource-group slapp-resource-group`
* To recreate from scratch (this should also re-pull the image)
  * `az container create --resource-group slapp-resource-group --name mavis --image slate.azurecr.io/mavis:latest`
  * The username is slate, and the password is in the ACR access keys.
* To start:
  * `az container start --name mavis --resource-group slapp-resource-group`

### Azure Cloud setup from scratch
```shell
ACR_NAME=slate.azurecr.io
SERVICE_PRINCIPAL_NAME=acr-service-principal
ACR_REGISTRY_ID=$(az acr show --name $ACR_NAME --query id --output tsv)
SP_APP_ID=$(az ad sp show --id http://$SERVICE_PRINCIPAL_NAME --query appId --output tsv)
echo "Service principal ID: $SP_APP_ID"
SP_PASSWD=$(az ad sp create-for-rbac --name http://$SERVICE_PRINCIPAL_NAME --scopes $ACR_REGISTRY_ID --role acrpull --query password --output tsv)
echo "Service principal password: $SP_PASSWD"
az container create --resource-group slapp-resource-group --name mavis --image slate.azurecr.io/mavis
```
