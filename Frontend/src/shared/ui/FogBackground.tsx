interface FogBackgroundProps {
  backgroundImage?: string;
}

export function FogBackground({ backgroundImage }: FogBackgroundProps) {
  return (
    <div className="fixed inset-0 overflow-hidden pointer-events-none" aria-hidden="true">
      {backgroundImage && (
        <div
          className="absolute inset-0 bg-cover bg-center bg-no-repeat"
          style={{ backgroundImage: `url(${backgroundImage})` }}
        />
      )}
      <div className="absolute inset-[-20%] rounded-full bg-ash-800/40 blur-3xl will-change-transform animate-fog-1" />
      <div className="absolute inset-[-15%] rounded-full bg-ash-900/30 blur-2xl will-change-transform animate-fog-2" />
      <div className="absolute inset-[-25%] rounded-full bg-ash-700/20 blur-3xl will-change-transform animate-fog-3" />
    </div>
  );
}
