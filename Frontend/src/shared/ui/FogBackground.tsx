interface FogBackgroundProps {
  backgroundImage?: string;
}

export function FogBackground({ backgroundImage }: FogBackgroundProps) {
  return (
    <div className="fixed inset-0 z-0 overflow-hidden pointer-events-none" aria-hidden="true">
      {backgroundImage && (
        <div
          className="absolute inset-0 bg-cover bg-center bg-no-repeat"
          style={{ backgroundImage: `url(${backgroundImage})` }}
        />
      )}
      {/* Small distinct wisps that drift across parts of the screen */}
      <div className="absolute w-[40%] h-[30%] top-[10%] left-[-10%] z-10 rounded-full bg-parchment-400 blur-3xl will-change-transform animate-fog-1" />
      <div className="absolute w-[35%] h-[25%] top-[50%] right-[-5%] z-10 rounded-full bg-parchment-500 blur-2xl will-change-transform animate-fog-2" />
      <div className="absolute w-[45%] h-[20%] bottom-[5%] left-[20%] z-10 rounded-full bg-parchment-300 blur-3xl will-change-transform animate-fog-3" />
      <div className="absolute w-[30%] h-[25%] top-[30%] left-[40%] z-10 rounded-full bg-parchment-400 blur-2xl will-change-transform animate-fog-2" />
    </div>
  );
}
