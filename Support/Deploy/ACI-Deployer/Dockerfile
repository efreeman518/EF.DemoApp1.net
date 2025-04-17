FROM mcr.microsoft.com/dotnet/sdk:9.0

RUN apt-get update -yq && \
    apt-get install -yq curl unzip gnupg ca-certificates lsb-release && \
    curl -sL https://aka.ms/InstallAzureCLIDeb | bash && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

CMD [ "sleep", "3600" ]
