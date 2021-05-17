FROM gitpod/workspace-dotnet:latest

USER gitpod

# Install & use custom Node.js version
ENV NODE_VERSION=14
RUN bash -c ". .nvm/nvm.sh && \
        nvm install ${NODE_VERSION} && \
        nvm alias default ${NODE_VERSION}"

RUN npm i -g npm@7
