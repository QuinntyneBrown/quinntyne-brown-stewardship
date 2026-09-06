# Bundled fonts

Karla and Newsreader are self-hosted under the accompanying OFL licenses.
Newsreader retains all 224 source Unicode mappings and OpenType layout features.
Its weight axis is limited to 300–600, matching the
range declared in `fonts.css` and the design system's light, normal and strong
tokens. The optical size is fixed at the font's default of 18; the design system's
optical-sizing token is `none`. This retains the default letter design at every
viewport and avoids transferring unused optical masters and weights 200 and 800.

To reproduce the optimization from an original Newsreader variable font with
FontTools and its WOFF2 dependency installed:

```sh
fonttools varLib.instancer newsreader-source.woff2 wght=300:400:600 opsz=18 --output=newsreader-limited.woff2
pyftsubset newsreader-limited.woff2 --unicodes='*' --layout-features='*' --flavor=woff2 --output-file=newsreader.woff2
```

The optimized asset is 50,548 bytes instead of 132,000. Source and optimized glyphs
were compared at weights 300, 400 and 600 with optical size 18: all characters
remain, advance widths differ by less than one font unit and outline coordinates
differ by less than two font units due to instancing roundoff.
