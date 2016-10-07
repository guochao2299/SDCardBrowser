#include <SPI.h>
#include <SD.h>

File root;

#define OPEN_DIR_COMMAND "OPEN"
#define LST_COMMAND "LIST"

#define NUM_LENGTH 4
#define SPLIT_CHAR ';'

#define RECEIVE_STAGE 0   // 接收命令阶段
#define EXECUTE_STAGE 1   // 执行命令阶段

#define IS_DIR "1"
#define IS_FILE "0"

#define MAX_CHARS 49

char buffer[MAX_CHARS + 1];
int charIndex = 0;

int currentStage;

void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
  while (!Serial)
  {
    ; // wait for serial port to connect. Needed for native USB port only
  }

  InitSDCard();
  printDirectory(root);
  currentStage = RECEIVE_STAGE;
  Serial.println("Ready");
}

void loop() {
  // put your main code here, to run repeatedly:
  switch (currentStage)
  {
    case RECEIVE_STAGE:
      ReceiveCommand();
      break;

    case EXECUTE_STAGE:
      if (strncmp(buffer, OPEN_DIR_COMMAND, NUM_LENGTH) == 0)
      {
        char* pDir = &buffer[NUM_LENGTH + 1];
        File f = SD.open(pDir);
        printDirectory(f);
        f.close();
      }

      currentStage = RECEIVE_STAGE;
      break;
  }
}

void InitSDCard()
{
  Serial.print("Initializing SD card...");

  if (!SD.begin(4)) {
    Serial.println("initialization failed!");
    return;
  }
  Serial.println("initialization done.");

  root = SD.open("/");
}

void printDirectory(File dir) {
  dir.rewindDirectory();

  while (true) {

    File entry =  dir.openNextFile();
    if (! entry) {
      // no more files
      break;
    }

    Serial.print(LST_COMMAND);
    Serial.print(":");
    Serial.print(entry.name());
    Serial.print(":");
    if (entry.isDirectory())
    {
      Serial.println(IS_DIR);
    } else
    {
      // files have sizes, directories do not
      Serial.println(IS_FILE);
    }
    entry.close();
  }
}

void ReceiveCommand()
{
  if (Serial.available() > 0)
  {
    char ch = Serial.read();

    //    Serial.print("received char is ");
    //    Serial.println(ch);

    if ((charIndex < MAX_CHARS) && (ch != SPLIT_CHAR))
    {
      buffer[charIndex++] = ch;
    }
    else
    {
      buffer[charIndex] = 0;
      charIndex = 0;
      currentStage = EXECUTE_STAGE;

      Serial.print("received command is ");
      Serial.println(buffer);
    }
  }
}
