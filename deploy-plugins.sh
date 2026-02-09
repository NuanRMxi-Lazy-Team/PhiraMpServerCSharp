#!/bin/bash

# Plugin deployment script
# This script copies plugin DLLs to the server's plugins directory

SERVER_DIR="PhiraMp.Server/bin/Debug/net10.0"
PLUGINS_DIR="$SERVER_DIR/plugins"

echo "Deploying plugins..."

# Create plugins directory if it doesn't exist
mkdir -p "$PLUGINS_DIR"

# Copy CommandPlugin
if [ -f "PhiraMp.Plugins.CommandPlugin/bin/Debug/net10.0/PhiraMp.Plugins.CommandPlugin.dll" ]; then
    cp "PhiraMp.Plugins.CommandPlugin/bin/Debug/net10.0/PhiraMp.Plugins.CommandPlugin.dll" "$PLUGINS_DIR/"
    echo "✓ CommandPlugin deployed"
else
    echo "✗ CommandPlugin not found - build the solution first"
fi

# Copy CycleVotingPlugin
if [ -f "PhiraMp.Plugins.CycleVoting/bin/Debug/net10.0/PhiraMp.Plugins.CycleVoting.dll" ]; then
    cp "PhiraMp.Plugins.CycleVoting/bin/Debug/net10.0/PhiraMp.Plugins.CycleVoting.dll" "$PLUGINS_DIR/"
    echo "✓ CycleVotingPlugin deployed"
else
    echo "✗ CycleVotingPlugin not found - build the solution first"
fi

echo ""
echo "Plugin deployment complete!"
echo "Plugins directory: $PLUGINS_DIR"
echo ""
echo "To run the server with plugins:"
echo "  cd $SERVER_DIR"
echo "  dotnet PhiraMp.Server.dll"
