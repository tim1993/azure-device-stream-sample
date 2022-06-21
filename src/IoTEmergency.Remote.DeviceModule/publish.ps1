az acr login -n waschingmachineeuwacr

dotnet build --configuration=Release -o ./out
docker build -t waschingmachineeuwacr.azurecr.io/remotemodule:latest . --platform linux/arm/v7
docker push waschingmachineeuwacr.azurecr.io/remotemodule:latest