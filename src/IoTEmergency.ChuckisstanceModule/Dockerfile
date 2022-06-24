FROM mcr.microsoft.com/dotnet/runtime:6.0
RUN useradd module
RUN mkdir /home/module
USER root
COPY ["out/**", "/home/module"]
ENTRYPOINT [ "dotnet", "/home/module/IoTEmergency.ChuckisstanceModule.dll"]