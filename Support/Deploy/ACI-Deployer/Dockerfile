FROM mcr.microsoft.com/azure-cli:latest

# Install additional tools (curl, unzip)
RUN apt-get update && \
    apt-get install -y unzip curl && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Set the default command to bash
CMD [ "bash" ]
