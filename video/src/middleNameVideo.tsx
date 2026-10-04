import React from 'react';
import {AbsoluteFill, Img, Series, staticFile, interpolate, useCurrentFrame} from 'remotion';
import {theme} from './theme';
import {Scene, Eyebrow, Title, StatTile, Rise} from './components';

/**
 * Stills are real Playwright screenshots (1920x1080) of the deployed app, written to
 * public/middle-name/ by the capture script. Names shown in captions come from props so
 * the composition matches whichever employees were captured.
 */
export type MiddleNameProps = {
  environment: string;
  release: string;
  withMiddleName: {fullName: string};
  withoutMiddleName: {userName: string};
};

export const MIDDLE_NAME_DEFAULTS: MiddleNameProps = {
  environment: 'Production',
  release: '2.4.18',
  withMiddleName: {fullName: 'Ima Marie Test'},
  withoutMiddleName: {userName: 'mpeck'},
};

const FADE = 10;

const Fade: React.FC<{durationInFrames: number; children: React.ReactNode}> = ({
  durationInFrames,
  children,
}) => {
  const frame = useCurrentFrame();
  const opacity = interpolate(
    frame,
    [0, FADE, durationInFrames - FADE, durationInFrames],
    [0, 1, 1, 0],
    {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'}
  );
  return <AbsoluteFill style={{opacity}}>{children}</AbsoluteFill>;
};

const Caption: React.FC<{label: string; text: string}> = ({label, text}) => (
  <div
    style={{
      position: 'absolute',
      left: 110,
      right: 110,
      bottom: 64,
      fontFamily: theme.font,
      background: 'rgba(13,13,13,0.86)',
      border: `1px solid ${theme.border}`,
      borderRadius: 12,
      padding: '18px 26px',
    }}
  >
    <div
      style={{
        fontSize: 20,
        letterSpacing: 3,
        textTransform: 'uppercase',
        fontWeight: 600,
        color: theme.series1,
      }}
    >
      {label}
    </div>
    <div style={{fontSize: 32, lineHeight: 1.4, color: theme.ink, marginTop: 6}}>{text}</div>
  </div>
);

/** A screenshot that slowly zooms toward a focus point given in percent of the frame. */
const Still: React.FC<{
  file: string;
  durationInFrames: number;
  focus?: [number, number];
  zoom?: number;
  label: string;
  text: string;
}> = ({file, durationInFrames, focus = [50, 50], zoom = 1.06, label, text}) => {
  const frame = useCurrentFrame();
  const scale = interpolate(frame, [20, durationInFrames - 30], [1, zoom], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  return (
    <AbsoluteFill style={{backgroundColor: theme.plane}}>
      <Img
        src={staticFile(`middle-name/${file}`)}
        style={{
          width: '100%',
          height: '100%',
          transform: `scale(${scale})`,
          transformOrigin: `${focus[0]}% ${focus[1]}%`,
        }}
      />
      <Caption label={label} text={text} />
    </AbsoluteFill>
  );
};

const HEADER_FOCUS: [number, number] = [96, 3];
const LOGIN_FOCUS: [number, number] = [57, 62];

const INTRO = 120;
const LOGIN = 150;
const WITH = 210;
const WITHOUT = 180;
const OUTRO = 180;

export const MIDDLE_NAME_TOTAL_FRAMES = INTRO + LOGIN + WITH + WITHOUT + OUTRO;

export const MiddleNameVideo: React.FC<MiddleNameProps> = ({
  environment,
  release,
  withMiddleName,
  withoutMiddleName,
}) => (
  <AbsoluteFill style={{backgroundColor: theme.plane}}>
    <Series>
      <Series.Sequence durationInFrames={INTRO}>
        <Fade durationInFrames={INTRO}>
          <Scene>
            <Rise>
              <Eyebrow color={theme.series1}>
                Work item #6 · Release {release} · {environment}
              </Eyebrow>
            </Rise>
            <Rise delay={8} style={{marginTop: 24}}>
              <Title>Employee middle name</Title>
            </Rise>
            <Rise delay={18} style={{marginTop: 28}}>
              <div style={{fontSize: 36, color: theme.inkSecondary, lineHeight: 1.4, maxWidth: 1300}}>
                Employees can now have an optional middle name. When they log in, the header
                greets them by their full name.
              </div>
            </Rise>
          </Scene>
        </Fade>
      </Series.Sequence>
      <Series.Sequence durationInFrames={LOGIN}>
        <Fade durationInFrames={LOGIN}>
          <Still
            file="with-2-selected.png"
            durationInFrames={LOGIN}
            focus={LOGIN_FOCUS}
            zoom={1.7}
            label={`${environment} · Login`}
            text={`Selecting ${withMiddleName.fullName}, an employee who has a middle name.`}
          />
        </Fade>
      </Series.Sequence>
      <Series.Sequence durationInFrames={WITH}>
        <Fade durationInFrames={WITH}>
          <Still
            file="with-3-loggedin.png"
            durationInFrames={WITH}
            focus={HEADER_FOCUS}
            zoom={2.2}
            label={`${environment} · Logged in`}
            text={`The header shows "Welcome ${withMiddleName.fullName}!" — first, middle and last name.`}
          />
        </Fade>
      </Series.Sequence>
      <Series.Sequence durationInFrames={WITHOUT}>
        <Fade durationInFrames={WITHOUT}>
          <Still
            file="without-3-loggedin.png"
            durationInFrames={WITHOUT}
            focus={HEADER_FOCUS}
            zoom={2.2}
            label={`${environment} · No middle name`}
            text={`Unchanged for employees without one: "Welcome ${withoutMiddleName.userName}!"`}
          />
        </Fade>
      </Series.Sequence>
      <Series.Sequence durationInFrames={OUTRO}>
        <Fade durationInFrames={OUTRO}>
          <Scene>
            <Rise>
              <Eyebrow color={theme.series3}>Delivered</Eyebrow>
            </Rise>
            <Rise delay={8} style={{marginTop: 20}}>
              <Title size={64}>
                Shipped in release {release}: live in TDD, UAT and {environment}
              </Title>
            </Rise>
            <div style={{display: 'flex', gap: 28, marginTop: 56}}>
              <StatTile value="032" label="Database migration" sub="MiddleName column, optional" delay={18} />
              <StatTile value="144" label="Acceptance tests passed in TDD" sub="0 failed" delay={26} />
              <StatTile value="#7" label="Pull request" sub="Merged to master" delay={34} />
            </div>
          </Scene>
        </Fade>
      </Series.Sequence>
    </Series>
  </AbsoluteFill>
);
