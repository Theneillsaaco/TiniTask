{
  description = "Mini TiniTask con Avalonia";
  
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };
  
  outputs = { self, nixpkgs }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
    in {
      devShells.${system}.default = pkgs.mkShell {
        packages = with pkgs; [ 
        dotnet-sdk_10
        fontconfig
        freetype
        libGL
        libX11
        libICE
        libSM
        libXext
        libXi
        libXrandr
        libXrender
        libxcb
        glib
        gtk3
        pango
        cairo
        harfbuzz
        icu
        zlib
        openssl
        ];
        
        LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath [
          pkgs.fontconfig
          pkgs.freetype
          pkgs.libGL
          pkgs.libX11
          pkgs.glib
          pkgs.gtk3
          pkgs.pango
          pkgs.cairo
          pkgs.harfbuzz
        ];
      };
    };
}