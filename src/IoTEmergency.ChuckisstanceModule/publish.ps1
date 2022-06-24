az acr login -n waschingmachineeuwacr

dotnet build --configuration=Release -o ./out
docker build -t waschingmachineeuwacr.azurecr.io/chuckisstance:latest . --platform linux/arm/v7
docker push waschingmachineeuwacr.azurecr.io/chuckisstance:latest