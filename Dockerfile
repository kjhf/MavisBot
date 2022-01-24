FROM mcr.microsoft.com/dotnet/sdk:5.0

# Now continue ...
WORKDIR /usr/
COPY . .

# Copy SplatTag
# GrabSplatTag.sh should be run first (as this puts the needed files into the build context... Docker is dumb.)
# ApplicationData is /home/usr/
COPY publish/net5.0 /usr/MavisApp/net5.0
COPY publish/Snapshot-*.json /usr/src/SplatTag/
# COPY publish/dola-gsheet-access-*.json /usr/src/SplatTag/

# Mavis env (the rest are provided by the .env file)
ENV SLAPP_DATA_FOLDER=/usr/src/SplatTag/

WORKDIR /usr/MavisApp/net5.0/
ENTRYPOINT ["dotnet", "Mavis.dll"]
