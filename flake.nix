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
        libICE
        libSM
        libGL
        libX11
        libXext
        libXrender
        libXi
        libXrandr
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
          pkgs.libICE
          pkgs.libSM
          pkgs.libX11
          pkgs.libXext
          pkgs.libXrender
          pkgs.libXi
          pkgs.libXrandr
          pkgs.libxcb
          pkgs.libGL
          pkgs.fontconfig
          pkgs.freetype
          pkgs.harfbuzz
          pkgs.pango
          pkgs.cairo
          pkgs.gtk3
          pkgs.glib
        ];
      };
    };
}