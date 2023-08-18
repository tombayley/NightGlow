using System;

namespace NightGlow.Models;

public readonly partial record struct ColorConfiguration(int Temperature, double Brightness)
{

    public static ColorConfiguration Default { get; } = new(6500, 1);

    public ColorConfiguration Offset(int temperatureOffset, double brightnessOffset) => new(
        Temperature + temperatureOffset,
        Brightness + brightnessOffset
    );

    public ColorConfiguration Clamp(int tempMin, int tempMax, double brightnessMin, double brightnessMax) =>
        new(
            Math.Clamp(Temperature, tempMin, tempMax),
            Math.Clamp(Brightness, brightnessMin, brightnessMax)
        );

}
